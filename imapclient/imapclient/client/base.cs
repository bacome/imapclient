using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Connection state values. See <see cref="cIMAPClient.ConnectionState"/>.
    /// </summary>
    public enum eConnectionState
    {
        /**<summary>The instance is not connected and never has been.</summary>*/
        notconnected,
        /**<summary>The instance is in the process of connecting.</summary>*/
        connecting,
        /**<summary>The instance is in the process of connecting, it is currently not authenticated.</summary>*/
        notauthenticated,
        /**<summary>The instance is in the process of connecting, it is currently authenticated.</summary>*/
        authenticated,
        /**<summary>The instance is in the process of connecting, it is has enabled all the server features it is going to.</summary>*/
        enabled,
        /**<summary>The instance connected, there is no mailbox selected.</summary>*/
        notselected,
        /**<summary>The instance connected, there is a mailbox selected.</summary>*/
        selected,

        /// <summary>
        /// <para>The instance is not connected, but it was connected, or tried to connect, once.</para>
        /// <para>
        /// In this state some <see cref="cIMAPClient"/> properties retain their values from when the instance was connecting/ was connected.
        /// For example the <see cref="cIMAPClient.Capabilities"/> property may have a value in this state, whereas it definitely won't have one in the <see cref="notconnected"/> state.
        /// </para>
        /// </summary>
        disconnected
    }

    /// <summary>
    /// Instances of this class can connect to an IMAP server.
    /// </summary>
    /// <remarks>
    /// <para>Before calling <see cref="Connect"/> set the <see cref="Server"/> and <see cref="Credentials"/> properties at a minimum.</para>
    /// <para>Also consider setting the <see cref="MailboxCacheData"/> property.</para>
    /// <para>Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// </remarks>
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

        /**<summary>The version number of the library. Used in the default <see cref="ClientId"/> value.</summary>*/
        public static Version Version = new Version(0, 3);

        /**<summary>The release date of the library. Used in the default <see cref="ClientId"/> value.</summary>*/
        public static DateTime ReleaseDate = new DateTime(2017, 11, 1);

        /**<summary>The trace source name used when tracing. See <see cref="cTrace"/>.</summary>*/
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
        /// Initialises a new instance, optionally specifying the instance name used in the tracing done by the instance (see <see cref="cTrace"/>).
        /// </summary>
        /// <param name="pInstanceName">The tracing instance name to use. See <see cref="cTrace"/>.</param>
        public cIMAPClient(string pInstanceName = TraceSourceName)
        {
            mInstanceName = pInstanceName;
            mRootContext = mTrace.NewRoot(pInstanceName);
            mRootContext.TraceInformation("cIMAPClient by bacome version {0}, release date {1}", Version, ReleaseDate);
            mSynchroniser = new cCallbackSynchroniser(this, mRootContext);
            mCancellationManager = new cCancellationManager(mSynchroniser.InvokeCancellableCountChanged);
        }

        /// <summary>
        /// Gets the instance name used in tracing done by the instance. See <see cref="cTrace"/>. Set using the constructor.
        /// </summary>
        public string InstanceName => mInstanceName;

        /// <summary>
        /// The <see cref="System.Threading.SynchronizationContext"/> on which callbacks (including events) are made. May be set to null.
        /// </summary>
        /// <remarks>
        /// <para>If set to null callbacks are made by the thread that discovers the need to do the callback.</para>
        /// <para>Defaults to the <see cref="System.Threading.SynchronizationContext"/> of the instantiating thread.</para>
        /// </remarks>
        public SynchronizationContext SynchronizationContext
        {
            get => mSynchroniser.SynchronizationContext;
            set => mSynchroniser.SynchronizationContext = value;
        }

        /// <summary>
        /// Fired when a property value changes.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { mSynchroniser.PropertyChanged += value; }
            remove { mSynchroniser.PropertyChanged -= value; }
        }

        /// <summary>
        /// Fired when the server sends response text.
        /// </summary>
        /// <remarks>
        /// <para>The IMAP spec says that <see cref="eResponseTextCode.alert"/> text MUST be brought to the user's attention. See <see cref="cResponseTextEventArgs.Text"/>.</para>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
        public event EventHandler<cResponseTextEventArgs> ResponseText
        {
            add { mSynchroniser.ResponseText += value; }
            remove { mSynchroniser.ResponseText -= value; }
        }

        /// <summary>
        /// Fired when the server sends a response.
        /// </summary>
        /// <remarks>
        /// <para>Intended for debugging the library.</para>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
        public event EventHandler<cNetworkReceiveEventArgs> NetworkReceive
        {
            add { mSynchroniser.NetworkReceive += value; }
            remove { mSynchroniser.NetworkReceive -= value; }
        }

        /// <summary>
        /// Fired when the client sends an IMAP command.
        /// </summary>
        /// <remarks>
        /// <para>Intended for debugging the library.</para>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
        public event EventHandler<cNetworkSendEventArgs> NetworkSend
        {
            add { mSynchroniser.NetworkSend += value; }
            remove { mSynchroniser.NetworkSend -= value; }
        }

        /// <summary>
        /// Fired when the backing data of a <see cref="cMailbox"/> property changes.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
        public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged
        {
            add { mSynchroniser.MailboxPropertyChanged += value; }
            remove { mSynchroniser.MailboxPropertyChanged -= value; }
        }

        /// <summary>
        /// Fired when the server sends notification of new messages in a <see cref="cMailbox"/>.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
        public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery
        {
            add { mSynchroniser.MailboxMessageDelivery += value; }
            remove { mSynchroniser.MailboxMessageDelivery -= value; }
        }

        /// <summary>
        /// Fired when the backing data of a <see cref="cMessage"/> property changes.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
        /// </remarks>
        public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged
        {
            add { mSynchroniser.MessagePropertyChanged += value; }
            remove { mSynchroniser.MessagePropertyChanged -= value; }
        }

        /// <summary>
        /// Fired when an exception is raised by a callback or event handler.
        /// </summary>
        /// <remarks>
        /// <para>The library ignores the exception other than raising this event.</para>
        /// <para>Intended for debugging consuming software.</para>
        /// <para>If <see cref="SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler of this event the exception is ignored.</para>
        /// </remarks>
        public event EventHandler<cCallbackExceptionEventArgs> CallbackException
        {
            add { mSynchroniser.CallbackException += value; }
            remove { mSynchroniser.CallbackException -= value; }
        }

        /// <summary>
        /// The timeout for library calls where no operation specific value for a timeout can be (or has been) specified.
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
        /// The number of currently running cancellable operations. See <see cref="Cancel"/> and <see cref="cCancellationManager"/>.
        /// </summary>
        public int CancellableCount => mCancellationManager.Count;

        /// <summary>
        /// Cancels the currently running cancellable operations. See <see cref="cCancellationManager"/> for more detail.
        /// </summary>
        public void Cancel()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Cancel));
            mCancellationManager.Cancel(lContext);
        }

        /**<summary>The connection state of the instance.</summary>*/
        public eConnectionState ConnectionState => mSession?.ConnectionState ?? eConnectionState.notconnected;
        /**<summary>True if the instance is currently unconnected. See <see cref="ConnectionState"/>.</summary>*/
        public bool IsUnconnected => mSession == null || mSession.IsUnconnected;
        /**<summary>True if the instance is currently connected. See <see cref="ConnectionState"/>.</summary>*/
        public bool IsConnected => mSession != null && mSession.IsConnected;

        /// <summary>
        /// The capabilities of the connected (or most recently connected) server. May be null.
        /// </summary>
        /// <remarks>
        /// The capabilities reflect the server capabilities less the <see cref="IgnoreCapabilities"/>.
        /// </remarks>
        public cCapabilities Capabilities => mSession?.Capabilities;

        /// <summary>
        /// The extensions that the library has enabled on the connected (or most recently connected) server.
        /// </summary>
        public fEnableableExtensions EnabledExtensions => mSession?.EnabledExtensions ?? fEnableableExtensions.none;

        /// <summary>
        /// The accountid of the current (or most recent) connection. May be null.
        /// </summary>
        public cAccountId ConnectedAccountId => mSession?.ConnectedAccountId;

        /// <summary>
        /// The login referral (RFC 2221), if received. May be null.
        /// </summary>
        public cURL HomeServerReferral => mSession?.HomeServerReferral;

        /// <summary>
        /// The server capabilities that the instance should ignore. Must be set before connecting. See <see cref="Capabilities"/> and <see cref="cCapabilities.EffectiveCapabilities"/>.
        /// </summary>
        /// <remarks>
        /// <para>Useful for testing or if your server (or the library) has a bug in its implementation of an IMAP extension.</para>
        /// </remarks>
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
        /// The server that the instance should connect to. There are helper methods to set this property. Must be set before calling <see cref="Connect"/>. Can only be set while unconnected: see <see cref="IsUnconnected"/>.
        /// </summary>
        /// <seealso cref="SetServer(string)"/>
        /// <seealso cref="SetServer(string, bool)"/>
        /// <seealso cref="SetServer(string, int, bool)"/>
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
        /// Sets <see cref="Server"/>, defaulting the port to 143 and SSL to false. Can only be called while unconnected: see <see cref="IsUnconnected"/>.
        /// </summary>
        /// <param name="pHost">The host name.</param>
        public void SetServer(string pHost) => Server = new cServer(pHost);

        /// <summary>
        /// Sets <see cref="Server"/>, specifying SSL, defaulting the port to 993 (SSL) or 143 (no SSL). Can only be called while unconnected: see <see cref="IsUnconnected"/>.
        /// </summary>
        /// <param name="pHost">The host name.</param>
        /// <param name="pSSL">Indicates if SSL should be used.</param>
        public void SetServer(string pHost, bool pSSL) => Server = new cServer(pHost, pSSL);

        /// <summary>
        /// Sets <see cref="Server"/>. Can only be called while unconnected: see <see cref="IsUnconnected"/>.
        /// </summary>
        /// <param name="pHost">The host name.</param>
        /// <param name="pPort">The port number.</param>
        /// <param name="pSSL">Indicates if SSL should be used.</param>
        public void SetServer(string pHost, int pPort, bool pSSL) => Server = new cServer(pHost, pPort, pSSL);

        /// <summary>
        /// The credentials to be used when connecting. There are helper methods to set this property. Must be set before calling <see cref="Connect"/>. Can only be set while unconnected: see <see cref="IsUnconnected"/>.
        /// </summary>
        /// <seealso cref="SetNoCredentials"/>
        /// <seealso cref="SetAnonymousCredentials(string, eTLSRequirement, bool)"/>
        /// <seealso cref="SetPlainCredentials(string, string, eTLSRequirement, bool)"/>
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
        /// Sets <see cref="Credentials"/> to no credentials. Can only be called while unconnected: see <see cref="IsUnconnected"/>.
        /// </summary>
        /// <remarks>
        /// Useful to retrieve property values of the server (e.g. <see cref="Capabilities"/>) without connecting, or when there is external authentication.
        /// </remarks>
        public void SetNoCredentials() => Credentials = cCredentials.None;

        /// <summary>
        /// Sets <see cref="Credentials"/> to anonymous credentials. Can only be called while unconnected: see <see cref="IsUnconnected"/>. May fall back to IMAP LOGIN if SASL ANONYMOUS isn't available.
        /// </summary>
        /// <param name="pTrace">The trace information sent to the server.</param>
        /// <param name="pTLSRequirement">The TLS requirement for these credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Try the SASL ANONYMOUS mechanism even if it isn't advertised.</param>
        public void SetAnonymousCredentials(string pTrace, eTLSRequirement pTLSRequirement = eTLSRequirement.indifferent, bool pTryAuthenticateEvenIfAnonymousIsntAdvertised = false) => Credentials = cCredentials.Anonymous(pTrace, pTLSRequirement, pTryAuthenticateEvenIfAnonymousIsntAdvertised);

        /// <summary>
        /// Sets <see cref="Credentials"/> to plain credentials. Can only be called while unconnected: see <see cref="IsUnconnected"/>. May fall back to IMAP LOGIN if SASL PLAIN isn't available.
        /// </summary>
        /// <param name="pUserId">The userid to use.</param>
        /// <param name="pPassword">The password to use.</param>
        /// <param name="pTLSRequirement">The TLS requirement for these credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Try the SASL PLAIN mechanism even if it isn't advertised.</param>
        public void SetPlainCredentials(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfPlainIsntAdvertised = false) => Credentials = cCredentials.Plain(pUserId, pPassword, pTLSRequirement, pTryAuthenticateEvenIfPlainIsntAdvertised);

        // not tested yet
        //public void SetXOAuth2Credentials(string pUserId, string pAccessToken, bool pTryAuthenticateEvenIfXOAuth2IsntAdvertised = false) => Credentials = cCredentials.XOAuth2(pUserId, pAccessToken, pTryAuthenticateEvenIfXOAuth2IsntAdvertised);

        /// <summary>
        /// Indicates if the calling program can handle mailbox referrals. If this is set to false the instance will not return remote mailboxes in mailbox lists.
        /// </summary>
        /// <remarks>
        /// <para>Being able to handle mailbox referrals means handling the exceptions that may be raised by the library when accessing remote mailboxes.</para>
        /// <para>See RFC 2193, <see cref="cUnsuccessfulCompletionException"/>, <see cref="cUnsuccessfulCompletionException.ResponseText"/>, <see cref="cResponseText.Strings"/>, <see cref="cURL"/>.</para>
        /// </remarks>
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
        /// Determines what details about mailboxes are requested from the server. Can only be set while unconnected: see <see cref="IsUnconnected"/>.
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
        /// Controls the size of writes to the network. You might want to limit this to increase the speed with which you can terminate the instance.
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
        /// Sets parameters that control what the instance does while idle. Set to null to stop the instance from doing anything. If set, the instance determines that it is idle after the specified time and then issues periodic IMAP IDLE (RFC 2177) or CHECK/ NOOP commands.
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
        /// The default control on the size of reads from streams provided to append. You might want to limit this to increase the speed with which you can terminate the instance.
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
        /// The configuration that controls the number of messages fetched at one time. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <seealso cref="Fetch(System.Collections.Generic.IEnumerable{cMessage}, cCacheItems, cPropertyFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(cFilter, cSort, cCacheItems, cMessageFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cCacheItems, cPropertyFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{iMessageHandle}, cCacheItems, cPropertyFetchConfiguration)"/>
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
        /// The configuration that controls the number of bytes fetched from the server at one time. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <seealso cref="cMessage.Fetch(cSection)"/>
        /// <seealso cref="cMessage.Fetch(cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
        /// <seealso cref="cMessage.Fetch(cSinglePartBody, System.IO.Stream, cBodyFetchConfiguration)"/>
        /// <seealso cref="cMessage.Fetch(cTextBodyPart)"/>
        /// <seealso cref="cAttachment.SaveAs(string, cBodyFetchConfiguration)"/>
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
        /// The configuration that controls the number of bytes written to the output stream at one time when fetching. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <seealso cref="cMessage.Fetch(cSection)"/>
        /// <seealso cref="cMessage.Fetch(cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
        /// <seealso cref="cMessage.Fetch(cSinglePartBody, System.IO.Stream, cBodyFetchConfiguration)"/>
        /// <seealso cref="cMessage.Fetch(cTextBodyPart)"/>
        /// <seealso cref="cAttachment.SaveAs(string, cBodyFetchConfiguration)"/>
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
        /// The encoding to use when RFC 6855 is not supported by the server. The default value is UTF8.
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
        /// The ASCII ID (RFC 2971) details to send to the server during <see cref="Connect"/>. If the server supports RFC 6855 and <see cref="ClientIdUTF8"/> is set, those details will be used in preference to these ones. The default details are those of the library. Set this to null to send nothing.
        /// </summary>
        /// <seealso cref="Connect"/>.
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
        /// The UTF8 ID (RFC 2971) details to send to the server during <see cref="Connect"/>. If this is null or if the server doesn't support RFC 6855 then <see cref="ClientId"/> is used instead. The default is null.
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
        /// The ID details of the connected (or last connected) server, if it supports the ID (RFC 2971). Set during <see cref="Connect"/>.
        /// </summary>
        public cId ServerId => mSession?.ServerId;

        /// <summary>
        /// The namespace details for the connected (or last connected) account. Set during <see cref="Connect"/>. If Namespace (RFC 2342) is not supported by the server the library creates one personal namespace using the delimiter retrieved using IMAP LIST.
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
        /// The inbox of the connected (or last connected) account. Set during <see cref="Connect"/>.
        /// </summary>
        public cMailbox Inbox => mInbox;

        /// <summary>
        /// Details of the currently selected mailbox. Will be null if there is no mailbox currently selected. See <see cref="cMailbox.Select(bool)"/>.
        /// </summary>
        public iSelectedMailboxDetails SelectedMailboxDetails => mSession?.SelectedMailboxDetails;

        /// <summary>
        /// The currently selected mailbox. Will be null if there is no mailbox currently selected. See <see cref="cMailbox.Select(bool)"/>.
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
        /// Returns an object that represents the named mailbox.
        /// </summary>
        /// <param name="pMailboxName">The mailbox name.</param>
        /// <returns></returns>
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
        /// Returns the number of subscriptions to the various events. Intended for debugging use. Use it to check that events are being 'unsubscribed' correctly.
        /// </summary>
        public sEventSubscriptionCounts EventSubscriptionCounts => mSynchroniser.EventSubscriptionCounts;

        /// <summary>
        /// Instances of this class contain a number of disposable resources. You should call dispose when you are finished with the instance.
        /// </summary>
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
