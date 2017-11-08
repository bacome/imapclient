using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public enum eConnectionState { notconnected, connecting, notauthenticated, authenticated, enabled, notselected, selected, disconnected }

    /// <summary>
    /// <para>Instances of this class can connect to an IMAP server.</para>
    /// <para>Before calling one of the <see cref="Connect"/> methods set the <see cref="Server"/> and <see cref="Credentials"/> properties at a minimum.</para>
    /// <para>See <see cref="SetServer(string, int, bool)"/> and <see cref="SetPlainCredentials(string, string, eTLSRequirement, bool)"/>.</para>
    /// <para>Also consider setting the <see cref="MailboxCacheData"/> property.</para>
    /// </summary>
    public sealed partial class cIMAPClient : IDisposable // sealed so the disposable implementation is simpler
    {
        // code checks
        //  check all awaits use configureawait(false)  quick and dirty: search for: await (?!.*\.ConfigureAwait\(false\).*\r?$) (this misses awaits with awaited parameters)   or this await [^(]*?\([^)]*\)[^.]
        //  ensure all changes to the 'state' property are done after the object is in the new state (so the event is fired with the object in a consistent state)
        //  check all 'Z' routines are private
        //  check that all async routines actually need to be: they don't if they can return the result of a called async routine
        //  check that the events are not fired directly in code
        //  check that all async methods on the imapclient increment and decrement the taskcounter
        //  check that all constants are named after the module that they are in: private static k<module>...
        //  when there is a message event and a mailbox event, the message event should fire before the mailbox event (this is just for consistency)

        // notes: (so I don't forget why)
        //
        //  the pipeline access system exists so that messages can be resolved to MSNs and to make select safe
        //   the problem is that if a command (other than FETCH, STORE, SEARCH - msnsafe commands) is in progress the server is allowed to send expunges: these invalidate message sequence numbers, 
        //    so the resolution of a message to its MSN has to take place while no msnUNsafe command is in progress and the resolved numbers have to be sent to the server before any msnUNsafe command is run
        //   we also don't want expunges or fetches arriving whilst selecting (do they apply to the old mailbox or the new one?)
        //  another problem is that if a command is in progress a UIDValidityChange can be sent by the server
        //   if we subsequently send (and note that this includes cases where the messages cross paths on the wire) an MSN or UID to the server, it may refer to the wrong message
        //   [note that this problem implies absolute single threading when dealing with message numbers,
        //     however rfc 3501 does not consider this possibility and explicitly encourages pipelining commands that expose the client to this problem,
        //     therefore I shall ignore this particular problem]
        //  the command completion hook exists so that returned MSNs can be resolved to messages, and to release locks held whilst commands are in progress
        //   MSN resolution has to be done at command completion because a subsequent command may send an expunge, invalidating the message numbers
        //   locks can't be released in the caller because the caller may time out AFTER the command is submitted (and the lock needs to be held until the server completes the command)
        //  the select lock is to make sure that the currently selected mailbox can be checked safely and to single thread select operations (so that the state on the client side is the same as the state on the server side)

        // notes on namepsaces
        //
        //  imapclient contains the classes and enums that I expect the user of the library will want to use
        //  imapclient.support contains the other things that have to be public but that aren't really intended for use outside the library
        //  trace is the tracing 
        //  async is generic async tools

        // notes on MDNSent
        //
        //  to implement MDNSent I need to not just recognise the MDNSent flag but also the fact that an MDN is required
        //   this involves getting and parsing the following headers;
        //    Disposition-Notification-To, Original-Recipient and Disposition-Notification-Options (see rfc 8098)
        //   the result of the parsing would be presented in an additional message attribute called MDNRequest which would be null if there are no headers
        //    or there are errors (like duplicate headers)
        //   so at this stage the MDNSent features are commented out as they aren't useful by themselves

        // version and release date of this class
        public static Version Version = new Version(0, 3);
        public static DateTime ReleaseDate = new DateTime(2017, 11, 1);

        public const string TraceSourceName = "work.bacome.cIMAPClient";
        private static readonly cTrace mTrace = new cTrace(TraceSourceName);

        // mechanics
        private bool mDisposed = false;
        private readonly string mInstanceName;
        private readonly cTrace.cContext mRootContext;
        private readonly cCallbackSynchroniser mSynchroniser;
        private readonly cCancellationManager mCancellationManager;

        // current session
        private cSession mSession = null;
        private cNamespaces mNamespaces = null; // if namespace is not supported by the server then this is used
        private cMailbox mInbox = null; // 

        // property backing storage
        private int mTimeout = -1;
        private fCapabilities mIgnoreCapabilities = 0;
        private cServer mServer = null;
        private cCredentials mCredentials = null;
        private bool mMailboxReferrals = false;
        private fMailboxCacheData mMailboxCacheData = fMailboxCacheData.messagecount | fMailboxCacheData.unseencount;
        private cBatchSizerConfiguration mNetworkWriteConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private cIdleConfiguration mIdleConfiguration = new cIdleConfiguration();
        private cBatchSizerConfiguration mAppendStreamReadConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private cBatchSizerConfiguration mFetchCacheItemsConfiguration = new cBatchSizerConfiguration(1, 1000, 10000, 1);
        private cBatchSizerConfiguration mFetchBodyReadConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private cBatchSizerConfiguration mFetchBodyWriteConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private Encoding mEncoding = Encoding.UTF8;
        private cClientId mClientId = new cClientId(new cIdDictionary(true));
        private cClientIdUTF8 mClientIdUTF8 = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pInstanceName">The instance name used to tag trace messages. Useful if you have multiple instances in one exe.</param>
        public cIMAPClient(string pInstanceName = TraceSourceName)
        {
            mInstanceName = pInstanceName;
            mRootContext = mTrace.NewRoot(pInstanceName);
            mRootContext.TraceInformation("cIMAPClient by bacome version {0}, release date {1}", Version, ReleaseDate);
            mSynchroniser = new cCallbackSynchroniser(this, mRootContext);
            mCancellationManager = new cCancellationManager(mSynchroniser.InvokeCancellableCountChanged);
        }

        /// <summary>
        /// <para>The instance name used to tag trace messages.</para>
        /// <para>Set using the constructor.</para>
        /// </summary>
        public string InstanceName => mInstanceName;

        /// <summary>
        /// <para>The synchronisation context on which callbacks (including events) are made.</para>
        /// <para>If set to null callbacks are made by the thread that discovers the need to do the callback.</para>
        /// </summary>
        public SynchronizationContext SynchronizationContext
        {
            get => mSynchroniser.SynchronizationContext;
            set => mSynchroniser.SynchronizationContext = value;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { mSynchroniser.PropertyChanged += value; }
            remove { mSynchroniser.PropertyChanged -= value; }
        }

        /// <summary>
        /// <para>Fired when the server sends response text.</para>
        /// <para>The IMAP spec says that alerts MUST be brought to the users attention.</para>
        /// </summary>
        public event EventHandler<cResponseTextEventArgs> ResponseText
        {
            add { mSynchroniser.ResponseText += value; }
            remove { mSynchroniser.ResponseText -= value; }
        }

        /// <summary>
        /// <para>Fired when the server sends a response.</para>
        /// </summary>
        public event EventHandler<cNetworkReceiveEventArgs> NetworkReceive
        {
            add { mSynchroniser.NetworkReceive += value; }
            remove { mSynchroniser.NetworkReceive -= value; }
        }

        /// <summary>
        /// <para>Fired when the client sends a command.</para>
        /// </summary>
        public event EventHandler<cNetworkSendEventArgs> NetworkSend
        {
            add { mSynchroniser.NetworkSend += value; }
            remove { mSynchroniser.NetworkSend -= value; }
        }

        /// <summary>
        /// <para>Fired when a <see cref="cMailbox"/> instance property changes.</para>
        /// </summary>
        public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged
        {
            add { mSynchroniser.MailboxPropertyChanged += value; }
            remove { mSynchroniser.MailboxPropertyChanged -= value; }
        }

        /// <summary>
        /// <para>Fired when new messages appear in a <see cref="cMailbox"/>.</para>
        /// </summary>
        public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery
        {
            add { mSynchroniser.MailboxMessageDelivery += value; }
            remove { mSynchroniser.MailboxMessageDelivery -= value; }
        }

        /// <summary>
        /// <para>Fired when a <see cref="cMessage"/> instance property changes.</para>
        /// </summary>
        public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged
        {
            add { mSynchroniser.MessagePropertyChanged += value; }
            remove { mSynchroniser.MessagePropertyChanged -= value; }
        }

        /// <summary>
        /// <para>Fired when an exception is raised by a callback.</para>
        /// <para>The library ignores the exception other than raising this event.</para>
        /// </summary>
        public event EventHandler<cCallbackExceptionEventArgs> CallbackException
        {
            add { mSynchroniser.CallbackException += value; }
            remove { mSynchroniser.CallbackException -= value; }
        }

        /// <summary>
        /// <para>Sets the timeout for calls that involve network access.</para>
        /// </summary>
        public int Timeout
        {
            get => mTimeout;

            set
            {
                if (value < -1) throw new ArgumentOutOfRangeException();
                mTimeout = value;
            }
        }

        /// <summary>
        /// <para>Returns the number of currently running cancellable operations.</para>
        /// <para>See <see cref="Cancel"/>.</para>
        /// </summary>
        public int CancellableCount => mCancellationManager.Count;

        /// <summary>
        /// <para>Cancels currently running cancellable operations.</para>
        /// </summary>
        public void Cancel()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Cancel));
            mCancellationManager.Cancel(lContext);
        }

        // state

        public eConnectionState ConnectionState => mSession?.ConnectionState ?? eConnectionState.notconnected;
        public bool IsUnconnected => mSession == null || mSession.IsUnconnected;
        public bool IsConnected => mSession != null && mSession.IsConnected;

        /// <summary>
        /// <para>Returns the capabilities of the connected (or most recently connected) server.</para>
        /// </summary>
        public cCapabilities Capabilities => mSession?.Capabilities;

        /// <summary>
        /// <para>Returns the extensions that the library enabled on the connected (or most recently connected) server.</para>
        /// </summary>
        public fEnableableExtensions EnabledExtensions => mSession?.EnabledExtensions ?? fEnableableExtensions.none;

        /// <summary>
        /// <para>Returns the accountid of the current (or most recent) connection.</para>
        /// </summary>
        public cAccountId ConnectedAccountId => mSession?.ConnectedAccountId;

        /// <summary>
        /// <para>The login referral (rfc 2221), if received.</para>
        /// </summary>
        public cURL HomeServerReferral => mSession?.HomeServerReferral;

        /// <summary>
        /// <para>Capabilities that you wish the instance to ignore.</para>
        /// <para>Must be set before connecting.</para>
        /// <para>Useful for testing or if your server (or the library) has a bug in its implementation of an IMAP extension.</para>
        /// </summary>
        public fCapabilities IgnoreCapabilities
        {
            get => mIgnoreCapabilities;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                if ((value & fCapabilities.logindisabled) != 0) throw new ArgumentOutOfRangeException();
                mIgnoreCapabilities = value;
            }
        }

        /// <summary>
        /// <para>The server that the instance should connect to.</para>
        /// <para>Must be set before connecting.</para>
        /// <para>See <see cref="SetServer(string)"/>, <see cref="SetServer(string, bool)"/> or <see cref="SetServer(string, int, bool)"/>.</para>
        /// </summary>
        public cServer Server
        {
            get => mServer;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                mServer = value;
            }
        }

        /// <summary>
        /// <para>Sets the server that the instance should connect to, defaulting the port to 143 and SSL to false.</para>
        /// <para>Can't be called while connected.</para>
        /// </summary>
        public void SetServer(string pHost) => Server = new cServer(pHost);

        /// <summary>
        /// <para>Sets the server that the instance should connect to, specifying whether SSL is required or not.</para>
        /// <para>If SSL is required the port is set to 993, otherwise it is set to 143.</para>
        /// <para>Can't be called while connected.</para>
        /// </summary>
        public void SetServer(string pHost, bool pSSL) => Server = new cServer(pHost, pSSL);

        /// <summary>
        /// <para>Sets the server that the instance should connect to, specifying the port and whether SSL is required or not.</para>
        /// <para>Can't be called while connected.</para>
        /// </summary>
        public void SetServer(string pHost, int pPort, bool pSSL) => Server = new cServer(pHost, pPort, pSSL);

        /// <summary>
        /// <para>The credentials to be used to connect to the server.</para>
        /// <para>Must be set before connecting.</para>
        /// <para>See <see cref="SetNoCredentials"/>, <see cref="SetAnonymousCredentials"/>, <see cref="SetPlainCredentials"/>.</para>
        /// </summary>
        public cCredentials Credentials
        {
            get => mCredentials;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                mCredentials = value;
            }
        }

        /// <summary>
        /// <para>Sets no credentials to be used to connect to the server.</para>
        /// <para>Can't be called while connected.</para>
        /// <para>Useful to retrieve the capabilities of the server without connecting or when there is external authentication.</para>
        /// </summary>
        public void SetNoCredentials() => Credentials = cCredentials.None;

        /// <summary>
        /// <para>Sets anonymous credentials to be used to connect to the server.</para>
        /// <para>Can't be called while connected.</para>
        /// <para>May fall back to LOGIN if SASL ANONYMOUS isn't available.</para>
        /// </summary>
        /// <param name="pTrace">The trace information sent to the server.</param>
        /// <param name="pTLSRequirement"></param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Try SASL ANONYMOUS mechanism even if it isn't advertised.</param>
        public void SetAnonymousCredentials(string pTrace, eTLSRequirement pTLSRequirement = eTLSRequirement.indifferent, bool pTryAuthenticateEvenIfAnonymousIsntAdvertised = false) => Credentials = cCredentials.Anonymous(pTrace, pTLSRequirement, pTryAuthenticateEvenIfAnonymousIsntAdvertised);

        /// <summary>
        /// <para>Sets plain credentials to be used to connect to the server.</para>
        /// <para>Can't be called while connected.</para>
        /// <para>May fall back to LOGIN if SASL PLAIN isn't available.</para>
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement"></param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Try SASL PLAIN mechanism even if it isn't advertised.</param>
        public void SetPlainCredentials(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfPlainIsntAdvertised = false) => Credentials = cCredentials.Plain(pUserId, pPassword, pTLSRequirement, pTryAuthenticateEvenIfPlainIsntAdvertised);

        // not tested yet
        //public void SetXOAuth2Credentials(string pUserId, string pAccessToken, bool pTryAuthenticateEvenIfXOAuth2IsntAdvertised = false) => Credentials = cCredentials.XOAuth2(pUserId, pAccessToken, pTryAuthenticateEvenIfXOAuth2IsntAdvertised);

        /// <summary>
        /// <para>Indicates if the caller can handle mailbox referrals.</para>
        /// <para>If this is set to false the instance will not return remote mailboxes in mailbox lists.</para>
        /// <para>Being able to handle mailbox referrals means handling the exceptions that may be raised by the library when accessing remote mailboxes.</para>
        /// <para>See RFC 2193, <see cref="cUnsuccessfulCompletionException"/>, <see cref="cUnsuccessfulCompletionException.ResponseText"/>, <see cref="cResponseText.Strings"/>, <see cref="cURL"/> and <see cref="cURI"/>.</para>
        /// </summary>
        public bool MailboxReferrals
        {
            get => mMailboxReferrals;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                mMailboxReferrals = value;
            }
        }

        /// <summary>
        /// <para>Determines what details about mailboxes are available from the mailbox cache.</para>
        /// <para>Can't be set while connected.</para>
        /// </summary>
        public fMailboxCacheData MailboxCacheData
        {
            get => mMailboxCacheData;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                mMailboxCacheData = value;
            }
        }

        /// <summary>
        /// <para>Controls the size of writes to the network.</para>
        /// <para>You might want to limit this to increase the speed with which you can terminate the instance.</para>
        /// <para>Higher values are presumably more efficient.</para>
        /// </summary>
        public cBatchSizerConfiguration NetworkWriteConfiguration
        {
            get => mNetworkWriteConfiguration;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                mNetworkWriteConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// <para>Sets parameters that control what the instance does while idle.</para>
        /// <para>Set to null to stop the instance from doing anything.</para>
        /// <para>If set, the instance determines that it is idle after the specified time and then issues periodic IDLE (rfc 2177) or CHECK/ NOOP IMAP commands.</para>
        /// </summary>
        public cIdleConfiguration IdleConfiguration
        {
            get => mIdleConfiguration;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(IdleConfiguration), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mIdleConfiguration = value;
                mSession?.SetIdleConfiguration(value, lContext);
            }
        }

        /// <summary>
        /// <para>The default control on the size of reads from streams provided to append.</para>
        /// <para>You might want to limit this to increase the speed with which you can terminate the instance.</para>
        /// </summary>
        public cBatchSizerConfiguration AppendStreamReadConfiguration
        {
            get => mAppendStreamReadConfiguration;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(AppendStreamReadConfiguration), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mAppendStreamReadConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// <para>The configuration that controls the number of messages fetched at one time.</para>
        /// <para>You might want to limit this to increase the speed with which you can cancel the fetch.</para>
        /// </summary>
        public cBatchSizerConfiguration FetchCacheItemsConfiguration
        {
            get => mFetchCacheItemsConfiguration;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(FetchCacheItemsConfiguration), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mFetchCacheItemsConfiguration = value ?? throw new ArgumentNullException();
                mSession?.SetFetchCacheItemsConfiguration(value, lContext);
            }
        }

        /// <summary>
        /// <para>The configuration that controls the number of bytes fetched from the server at one time.</para>
        /// <para>You might want to limit this to increase the speed with which you can cancel the fetch.</para>
        /// </summary>
        public cBatchSizerConfiguration FetchBodyReadConfiguration
        {
            get => mFetchBodyReadConfiguration;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(FetchBodyReadConfiguration), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mFetchBodyReadConfiguration = value ?? throw new ArgumentNullException();
                mSession?.SetFetchBodyReadConfiguration(value, lContext);
            }
        }

        /// <summary>
        /// <para>The configuration that controls the number of bytes written to the output stream at one time when fetching.</para>
        /// <para>You might want to limit this to increase the speed with which you can cancel the fetch.</para>
        /// </summary>
        public cBatchSizerConfiguration FetchBodyWriteConfiguration
        {
            get => mFetchBodyWriteConfiguration;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(FetchBodyWriteConfiguration), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mFetchBodyWriteConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// <para>The encoding to use when UTF8 (rfc 6855) is not supported directly by the server.</para>
        /// <para>The default value is UTF8.</para>
        /// </summary>
        public Encoding Encoding
        {
            get => mEncoding;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(Encoding), value.WebName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!cCommandPartFactory.TryAsCharsetName(value.WebName, out _)) throw new ArgumentOutOfRangeException();
                mEncoding = value;
                mSession?.SetEncoding(value, lContext);
            }
        }

        /// <summary>
        /// <para>The ID details to send to the server if it supports the ID command (rfc 2971).</para>
        /// <para>This is the ASCII version of the details.</para>
        /// <para>If the server supports UTF8 and <see cref="ClientIdUTF8"/> is set, those details will be used in preference to these.</para>
        /// <para>The default details are those of the library.</para>
        /// <para>Set to null to send nothing.</para>
        /// </summary>
        public cClientId ClientId
        {
            get => mClientId;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                mClientId = value;
            }
        }

        /// <summary>
        /// <para>The ID details to send to the server if it supports the ID command (rfc 2971).</para>
        /// <para>This is the UTF8 version of the details.</para>
        /// <para>If this is set to null or the server does not support UTF8 then the <see cref="ClientId"/> details will be used instead.</para>
        /// <para>The default is null.</para>
        /// </summary>
        public cClientIdUTF8 ClientIdUTF8
        {
            get => mClientIdUTF8 ?? mClientId;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException();
                mClientIdUTF8 = value;
            }
        }

        /// <summary>
        /// <para>The ID details of the connected (or last connected) server, if it supports the ID command (rfc 2971).</para>
        /// <para>Set during connect.</para>
        /// </summary>
        public cId ServerId => mSession?.ServerId;

        /// <summary>
        /// <para>The namespace details for the connected (or last connected) account.</para>
        /// <para>Set during connect.</para>
        /// <para>If namespaces (rfc 2342) are not supported by the server the library creates one personal namespace using the delimiter retrieved using LIST.</para>
        /// </summary>
        public cNamespaces Namespaces
        {
            get
            {
                if (mNamespaces != null) return mNamespaces;
                var lNamespaceNames = mSession?.NamespaceNames;
                if (lNamespaceNames == null) return null;
                return new cNamespaces(this, lNamespaceNames.Personal, lNamespaceNames.OtherUsers, lNamespaceNames.Shared);
            }
        }

        /// <summary>
        /// <para>The inbox of the connected (or last connected) account.</para>
        /// <para>Set during connect.</para>
        /// </summary>
        public cMailbox Inbox => mInbox;

        /// <summary>
        /// <para>Details of the currently selected mailbox, if any.</para>
        /// </summary>
        public iSelectedMailboxDetails SelectedMailboxDetails => mSession?.SelectedMailboxDetails;

        /// <summary>
        /// <para>The currently selected mailbox, if any.</para>
        /// </summary>
        public cMailbox SelectedMailbox
        {
            get
            {
                var lDetails = mSession?.SelectedMailboxDetails;
                if (lDetails == null) return null;
                return new cMailbox(this, lDetails.Handle);
            }
        }

        /// <summary>
        /// <para>Returns an object that represents the named mailbox.</para>
        /// </summary>
        /// <param name="pMailboxName">The mailbox name.</param>
        /// <returns>An object representing the named mailbox.</returns>
        public cMailbox Mailbox(cMailboxName pMailboxName)
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            var lHandle = mSession.GetMailboxHandle(pMailboxName);

            return new cMailbox(this, lHandle);
        }

        internal bool? HasCachedChildren(iMailboxHandle pHandle) => mSession?.HasCachedChildren(pHandle);

        /// <summary>
        /// <para>Intended for debugging use.</para>
        /// <para>Returns the number of subscriptions to the various events.</para>
        /// <para>Used to check that events are being 'unsubscribed' correctly.</para>
        /// </summary>
        public sEventSubscriptionCounts EventSubscriptionCounts => mSynchroniser.EventSubscriptionCounts;

        public void Dispose()
        {
            if (mDisposed) return;

            if (mSession != null)
            {
                try { mSession.Dispose(); }
                catch { }
            }

            if (mSynchroniser != null)
            {
                try { mSynchroniser.Dispose(); }
                catch { }
            }

            if (mCancellationManager != null)
            {
                try { mCancellationManager.Dispose(); }
                catch { }
            }

            mDisposed = true;
        }
    }
}
