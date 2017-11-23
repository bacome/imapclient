using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using work.bacome.apidocumentation;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents the connection state of an IMAP client instance.
    /// </summary>
    /// <remarks>
    /// In the <see cref="disconnected"/> state some <see cref="cIMAPClient"/> properties retain their values from when the instance was connecting/ was connected.
    /// For example <see cref="cIMAPClient.Capabilities"/> may have a value in this state, whereas it definitely won't have one in the <see cref="notconnected"/> state.
    /// </remarks>
    /// <seealso cref="cIMAPClient.ConnectionState"/>
    public enum eConnectionState
    {
        /**<summary>The instance is not connected and never has been.</summary>*/
        notconnected,
        /**<summary>The instance is in the process of connecting.</summary>*/
        connecting,
        /**<summary>The instance is in the process of connecting, it is currently not authenticated.</summary>*/
        notauthenticated,
        /**<summary>The instance is in the process of connecting, it is authenticated.</summary>*/
        authenticated,
        /**<summary>The instance is in the process of connecting, it has enabled all the server features it is going to.</summary>*/
        enabled,
        /**<summary>The instance is connected, there is no mailbox selected.</summary>*/
        notselected,
        /**<summary>The instance is connected, there is a mailbox selected.</summary>*/
        selected,
        /**<summary>The instance is not connected, but it was connected, or tried to connect, once.</summary>*/
        disconnected
    }

    /// <summary>
    /// Instances of this class can interact with an IMAP server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An instance may connect many times, possibly to different servers, but it can only be connected to one server at a time.
    /// </para>
    /// <para>
    /// To connect to a server use <see cref="Connect"/>.
    /// Before calling <see cref="Connect"/> set <see cref="Server"/> and <see cref="Credentials"/> at a minimum.
    /// Also consider setting <see cref="MailboxCacheDataItems"/>.
    /// </para>
    /// <para>This class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
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


        // ......................................................................................................................... when changing the version here also change it in the assemblyinfo

        /**<summary>The version number of the library. Used in the default value of <see cref="ClientId"/>.</summary>*/
        public static Version Version = new Version(0, 5);

        /**<summary>The release date of the library. Used in the default value of <see cref="ClientId"/>.</summary>*/
        public static DateTime ReleaseDate = new DateTime(2017, 11, 23);

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
        private fMailboxCacheDataItems mMailboxCacheDataItems = fMailboxCacheDataItems.messagecount | fMailboxCacheDataItems.unseencount;
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
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pInstanceName">The instance name to use for the instance's <see cref="cTrace"/> root-context.</param>
        public cIMAPClient(string pInstanceName = TraceSourceName)
        {
            mInstanceName = pInstanceName;
            mRootContext = mTrace.NewRoot(pInstanceName);
            mRootContext.TraceInformation("cIMAPClient by bacome version {0}, release date {1}", Version, ReleaseDate);
            mSynchroniser = new cCallbackSynchroniser(this, mRootContext);
            mCancellationManager = new cCancellationManager(mSynchroniser.InvokeCancellableCountChanged);
        }

        /// <summary>
        /// Gets the instance name used in the tracing done by the instance.
        /// </summary>
        /// <seealso cref="cTrace"/>
        /// <seealso cref="cIMAPClient(String)"/>
        public string InstanceName => mInstanceName;

        /// <summary>
        /// Gets and sets the <see cref="System.Threading.SynchronizationContext"/> on which callbacks and events are invoked. May be set to <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If this property is not <see langword="null"/>, callbacks and events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>, otherwise they are invoked directly by the initiating library thread. 
        /// Defaults to the <see cref="System.Threading.SynchronizationContext"/> of the instantiating thread.
        /// </remarks>
        public SynchronizationContext SynchronizationContext
        {
            get => mSynchroniser.SynchronizationContext;
            set => mSynchroniser.SynchronizationContext = value;
        }

        /// <summary>
        /// Fired when a property value of the instance changes.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
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
        /// <para>
        /// If <see cref="SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler then the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </para>
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
        /// <para>This event is provided to aid in the debugging of the library.</para>
        /// <para>
        /// If <see cref="SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler then the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </para>
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
        /// <para>This event is provided to aid in the debugging of the library.</para>
        /// <para>
        /// If <see cref="SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler then the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </para>
        /// </remarks>
        public event EventHandler<cNetworkSendEventArgs> NetworkSend
        {
            add { mSynchroniser.NetworkSend += value; }
            remove { mSynchroniser.NetworkSend -= value; }
        }

        /// <summary>
        /// Fired when the server notifies the client of a change that could affect a property value of a <see cref="cMailbox"/> instance.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged
        {
            add { mSynchroniser.MailboxPropertyChanged += value; }
            remove { mSynchroniser.MailboxPropertyChanged -= value; }
        }

        /// <summary>
        /// Fired when the server notifies the client that messages have arrived in a mailbox.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery
        {
            add { mSynchroniser.MailboxMessageDelivery += value; }
            remove { mSynchroniser.MailboxMessageDelivery -= value; }
        }

        /// <summary>
        /// Fired when the server notifies the client of a change that could affect a property value of a <see cref="cMessage"/> instance.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged
        {
            add { mSynchroniser.MessagePropertyChanged += value; }
            remove { mSynchroniser.MessagePropertyChanged -= value; }
        }

        /// <summary>
        /// Fired when an exception is raised by external code in a callback or event handler.
        /// </summary>
        /// <remarks>
        /// <para>The library ignores the exception other than raising this event. This event is provided to aid in the debugging of external code.</para>
        /// <para>
        /// If <see cref="SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler of this event then the exception is completely ignored.
        /// </para>
        /// </remarks>
        public event EventHandler<cCallbackExceptionEventArgs> CallbackException
        {
            add { mSynchroniser.CallbackException += value; }
            remove { mSynchroniser.CallbackException -= value; }
        }

        /// <summary>
        /// Gets and sets the timeout for library calls where no operation specific value for a timeout can be (or has been) specified.
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
        /// Gets the current number of cancellable operations.
        /// </summary>
        /// <seealso cref="Cancel"/>
        /// <seealso cref="cCancellationManager"/>
        public int CancellableCount => mCancellationManager.Count;

        /// <summary>
        /// Cancels the current cancellable operations.
        /// </summary>
        /// <seealso cref="cCancellationManager.Cancel(cTrace.cContext)"/>
        public void Cancel()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Cancel));
            mCancellationManager.Cancel(lContext);
        }

        /// <summary>
        /// Gets the connection state of the instance.
        /// </summary>
        /// <seealso cref="IsConnected"/>
        /// <seealso cref="IsUnconnected"/>
        public eConnectionState ConnectionState => mSession?.ConnectionState ?? eConnectionState.notconnected;

        /// <summary>
        /// Indicates whether the instance is currently unconnected.
        /// </summary>
        /// <seealso cref="IsConnected"/>
        /// <seealso cref="ConnectionState"/>
        public bool IsUnconnected => mSession == null || mSession.IsUnconnected;

        /// <summary>
        /// Indicates whether the instance is currently connected.
        /// </summary>
        /// <seealso cref="IsUnconnected"/>
        /// <seealso cref="ConnectionState"/>
        public bool IsConnected => mSession != null && mSession.IsConnected;

        /// <summary>
        /// Gets the capabilities of the connected (or most recently connected) server. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The capabilities reflect the server capabilities less the <see cref="IgnoreCapabilities"/>.
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cCapabilities Capabilities => mSession?.Capabilities;

        /// <summary>
        /// Gets the extensions that the library has enabled on the connected (or most recently connected) server.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public fEnableableExtensions EnabledExtensions => mSession?.EnabledExtensions ?? fEnableableExtensions.none;

        /// <summary>
        /// Gets the accountid of the current (or most recent) connection. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cAccountId ConnectedAccountId => mSession?.ConnectedAccountId;

        /// <summary>
        /// Gets the login referral (RFC 2221), if received. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cURL HomeServerReferral => mSession?.HomeServerReferral;

        /// <summary>
        /// Gets and sets the server capabilities that the instance should ignore.
        /// </summary>
        /// <remarks>
        /// May only be set while <see cref="IsUnconnected"/>.
        /// Useful for testing or if your server (or the library) has a bug in its implementation of an IMAP extension.
        /// </remarks>
        /// <seealso cref="Capabilities"/>
        public fCapabilities IgnoreCapabilities
        {
            get => mIgnoreCapabilities;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                if ((value & fCapabilities.logindisabled) != 0) throw new ArgumentOutOfRangeException();
                mIgnoreCapabilities = value;
            }
        }

        /// <summary>
        /// Gets and sets the server to connect to. 
        /// </summary>
        /// <remarks>
        /// Must be set before calling <see cref="Connect"/>.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// </remarks>
        /// <seealso cref="SetServer(string)"/>
        /// <seealso cref="SetServer(string, bool)"/>
        /// <seealso cref="SetServer(string, int, bool)"/>
        public cServer Server
        {
            get => mServer;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mServer = value;
            }
        }

        /// <summary>
        /// Sets <see cref="Server"/>, defaulting the port to 143 and SSL to <see langword="false"/>. 
        /// </summary>
        /// <param name="pHost"></param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// </remarks>
        public void SetServer(string pHost) => Server = new cServer(pHost);

        /// <summary>
        /// Sets <see cref="Server"/>, defaulting the port to 143 (no SSL) or 993 otherwise.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pSSL">Indicates whether the host requires that TLS be established immediately upon connect.</param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// </remarks>
        public void SetServer(string pHost, bool pSSL) => Server = new cServer(pHost, pSSL);

        /// <summary>
        /// Sets <see cref="Server"/>.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pPort"></param>
        /// <param name="pSSL">Indicates whether the host requires that TLS be established immediately upon connect.</param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// </remarks>
        public void SetServer(string pHost, int pPort, bool pSSL) => Server = new cServer(pHost, pPort, pSSL);

        /// <summary>
        /// Gets and sets the credentials to be used by <see cref="Connect"/>.
        /// </summary>
        /// <remarks>
        /// Must be set before calling <see cref="Connect"/>. 
        /// May only be set while <see cref="IsUnconnected"/>.
        /// </remarks>
        /// <seealso cref="SetNoCredentials"/>
        /// <seealso cref="SetAnonymousCredentials(string, eTLSRequirement, bool)"/>
        /// <seealso cref="SetPlainCredentials(string, string, eTLSRequirement, bool)"/>
        public cCredentials Credentials
        {
            get => mCredentials;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mCredentials = value;
            }
        }

        /// <summary>
        /// Sets <see cref="Credentials"/> to no credentials. 
        /// </summary>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// Useful to retrieve the property values set during <see cref="Connect"/> without actually connecting.
        /// Also useful when there is external authentication.
        /// </remarks>
        /// <seealso cref="Connect"/>
        public void SetNoCredentials() => Credentials = cCredentials.None;

        /// <summary>
        /// Sets <see cref="Credentials"/> to anonymous credentials. 
        /// </summary>
        /// <param name="pTrace">The trace information to be sent to the server.</param>
        /// <param name="pTLSRequirement">The TLS requirement for the credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfAnonymousIsntAdvertised">Indicates whether the SASL ANONYMOUS mechanism should be tried even if not advertised.</param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// The credentials may fall back to IMAP LOGIN if SASL ANONYMOUS isn't available.
        /// This method will throw if <paramref name="pTrace"/> can be used in neither <see cref="cLogin.Password"/> nor <see cref="cSASLAnonymous"/>.
        /// </remarks>
        public void SetAnonymousCredentials(string pTrace, eTLSRequirement pTLSRequirement = eTLSRequirement.indifferent, bool pTryAuthenticateEvenIfAnonymousIsntAdvertised = false) => Credentials = cCredentials.Anonymous(pTrace, pTLSRequirement, pTryAuthenticateEvenIfAnonymousIsntAdvertised);

        /// <summary>
        /// Sets <see cref="Credentials"/> to plain credentials.
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for the credentials to be used.</param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Indicates whether the SASL PLAIN mechanism should be tried even if not advertised.</param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// The credentials may fall back to IMAP LOGIN if SASL PLAIN isn't available.
        /// This method will throw if the userid and password can be used in neither <see cref="cLogin"/> nor <see cref="cSASLPlain"/>.
        /// </remarks>
        public void SetPlainCredentials(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfPlainIsntAdvertised = false) => Credentials = cCredentials.Plain(pUserId, pPassword, pTLSRequirement, pTryAuthenticateEvenIfPlainIsntAdvertised);

        // not tested yet
        //public void SetXOAuth2Credentials(string pUserId, string pAccessToken, bool pTryAuthenticateEvenIfXOAuth2IsntAdvertised = false) => Credentials = cCredentials.XOAuth2(pUserId, pAccessToken, pTryAuthenticateEvenIfXOAuth2IsntAdvertised);

        /// <summary>
        /// Gets and sets whether mailbox referrals will be handled for the instance.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="false"/>.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// If this is set to <see langword="false"/> the instance will not return remote mailboxes in mailbox lists.
        /// Handling mailbox referrals means handling the exceptions that could be raised when accessing remote mailboxes.
        /// See RFC 2193 for details.
        /// </remarks>
        /// <seealso cref="cUnsuccessfulCompletionException"/>
        /// <seealso cref="cUnsuccessfulCompletionException.ResponseText"/>
        /// <seealso cref="cResponseText.Arguments"/>
        /// <seealso cref="cURL"/>
        /// <seealso cref="cURI"/>
        public bool MailboxReferrals
        {
            get => mMailboxReferrals;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mMailboxReferrals = value;
            }
        }
    
        /// <summary>
        /// Gets and sets the set of optionally requested mailbox data items for the instance.
        /// </summary>
        /// <remarks>
        /// The default set is <see cref="fMailboxCacheDataItems.messagecount"/> and <see cref="fMailboxCacheDataItems.unseencount"/>.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// <note type="note" >
        /// The mailbox data items that are actually requested depends on the <see cref="fMailboxCacheDataSets"/> value used at the time of the request.
        /// </note>
        /// </remarks>
        /// <seealso cref="cNamespace.Mailboxes(fMailboxCacheDataSets)"/>
        /// <seealso cref="cNamespace.Subscribed(bool, fMailboxCacheDataSets)"/>
        /// <seealso cref="cMailbox.Mailboxes(fMailboxCacheDataSets)"/>
        /// <seealso cref="cMailbox.Subscribed(bool, fMailboxCacheDataSets)"/>
        /// <seealso cref="cMailbox.Fetch(fMailboxCacheDataSets)"/>
        /// <seealso cref="iMailboxContainer.Mailboxes(fMailboxCacheDataSets)"/>
        /// <seealso cref="iMailboxContainer.Subscribed(bool, fMailboxCacheDataSets)"/>
        /// <seealso cref="Mailboxes(string, char?, fMailboxCacheDataSets)"/>
        /// <seealso cref="Subscribed(string, char?, bool, fMailboxCacheDataSets)"/>
        public fMailboxCacheDataItems MailboxCacheDataItems
        {
            get => mMailboxCacheDataItems;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mMailboxCacheDataItems = value;
            }
        }

        /// <summary>
        /// Gets and sets the network-write batch-size configuration. You might want to limit this to increase the speed with which you can terminate the instance. May only be set while <see cref="IsUnconnected"/>.
        /// </summary>
        /// <remarks>
        /// Limits the size of the buffer used when sending data to the server. Measured in bytes.
        /// The default value is min=1000b, max=1000000b, maxtime=10s, initial=1000b.
        /// </remarks>
        public cBatchSizerConfiguration NetworkWriteConfiguration
        {
            get => mNetworkWriteConfiguration;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mNetworkWriteConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// Gets and sets the instance idle configuration. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// For details of the idling process, see <see cref="cIdleConfiguration"/>.
        /// Set this property to <see langword="null"/> to prevent the instance from idling.
        /// The default value is a default instance of <see cref="cIdleConfiguration"/>.
        /// </remarks>
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
        /// Gets and sets the default append-stream-read batch-size configuration. You might want to limit this to increase the speed with which you can terminate the instance.
        /// </summary>
        /// <remarks>
        /// Limits the size of the buffer when reading from the client-side stream (e.g. when reading an attachment from local disk). Measured in bytes.
        /// The default value is min=1000b, max=1000000b, maxtime=10s, initial=1000b.
        /// </remarks>
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
        /// Gets and sets the fetch-cache-items batch-size configuration. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <remarks>
        /// Limits the number of messages per batch when requesting cache-items from the server. Measured in number of messages.
        /// The default value is min=1 message, max=1000 messages, maxtime=10s, initial=1 message.
        /// </remarks>
        /// <seealso cref="Fetch(System.Collections.Generic.IEnumerable{cMessage}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
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
        /// Gets and sets the fetch-body-read batch-size configuration. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <remarks>
        /// Limits the size of the partial fetches used when getting body sections from the server. Measured in bytes.
        /// The default value is min=1000b, max=1000000b, maxtime=10s, initial=1000b.
        /// </remarks>
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
        /// Gets and sets the fetch-body-write batch-size configuration. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <remarks>
        /// Limits the size of the buffer when writing to the client-side stream (e.g. when writing to the local disk). Measured in bytes.
        /// The default value is min=1000b, max=1000000b, maxtime=10s, initial=1000b.
        /// </remarks>
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
        /// Gets and sets the encoding to use when <see cref="fEnableableExtensions.utf8"/> is not enabled.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="Encoding.UTF8"/>.
        /// Only used when filtering by message content.
        /// If the connected server does not support the encoding it will reject filters that use it and the library will throw <see cref="cUnsuccessfulCompletionException"/> with <see cref="eResponseTextCode.badcharset"/>.
        /// </remarks>
        /// <seealso cref="cFilterPart"/>
        /// <seealso cref="cFilter.HeaderFieldContains(string, string)"/>
        /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
        /// <seealso cref="cUnsuccessfulCompletionException.ResponseText"/>
        /// <seealso cref="cResponseText.Code"/>
        /// <seealso cref="cResponseText.Arguments"/>
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
        /// Gets and sets the ASCII ID (RFC 2971) details. 
        /// </summary>
        /// <remarks>
        /// If <see cref="cCapabilities.Id"/> is in use, these details are sent to the server during <see cref="Connect"/>.
        /// If <see cref="fEnableableExtensions.utf8"/> has been enabled and <see cref="ClientIdUTF8"/> is not <see langword="null"/>, then <see cref="ClientIdUTF8"/> will be used in preference to the value of this property.
        /// The default value is details about the library.
        /// Set this and <see cref="ClientIdUTF8"/> to <see langword="null"/> to send nothing to the server.
        /// </remarks>
        public cClientId ClientId
        {
            get => mClientId;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mClientId = value;
            }
        }

        /// <summary>
        /// Gets and sets the UTF8 ID (RFC 2971) details.
        /// </summary>
        /// <remarks>
        /// If this property is <see langword="null"/> or <see cref="fEnableableExtensions.utf8"/> has not been enabled then <see cref="ClientId"/> is used instead.
        /// The default value of this property is <see langword="null"/>.
        /// See <see cref="cClientId"/> and/ or <see cref="Connect"/> for more details.
        /// </remarks>
        public cClientIdUTF8 ClientIdUTF8
        {
            get => mClientIdUTF8 ?? mClientId;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mClientIdUTF8 = value;
            }
        }

        /// <summary>
        /// Gets the ID (RFC 2971) details of the connected (or last connected) server, if they were sent. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cId ServerId => mSession?.ServerId;

        /// <summary>
        /// Gets the namespace details for the connected (or last connected) account.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>. Will be set to something even if <see cref="cCapabilities.Namespace"/> is not in use.
        /// </remarks>
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
        /// Gets the inbox of the connected (or last connected) account.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cMailbox Inbox => mInbox;

        /// <summary>
        /// Gets the details of the currently selected mailbox, or <see langword="null"/> if there is no mailbox currently selected.
        /// </summary>
        /// <remarks>
        /// Use <see cref="cMailbox.Select(bool)"/> to select a mailbox.
        /// </remarks>
        public iSelectedMailboxDetails SelectedMailboxDetails => mSession?.SelectedMailboxDetails;

        /// <summary>
        /// Gets an object that represents the currently selected mailbox, or <see langword="null"/> if there is no mailbox currently selected.
        /// </summary>
        /// <remarks>
        /// Use <see cref="cMailbox.Select(bool)"/> to select a mailbox.
        /// </remarks>
        public cMailbox SelectedMailbox
        {
            get
            {
                var lDetails = mSession?.SelectedMailboxDetails;
                if (lDetails == null) return null;
                return new cMailbox(this, lDetails.MailboxHandle);
            }
        }

        /// <summary>
        /// Gets an object that represents the named mailbox.
        /// </summary>
        /// <param name="pMailboxName"></param>
        /// <returns></returns>
        public cMailbox Mailbox(cMailboxName pMailboxName)
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            var lMailboxHandle = mSession.GetMailboxHandle(pMailboxName);

            return new cMailbox(this, lMailboxHandle);
        }

        internal bool? HasCachedChildren(iMailboxHandle pMailboxHandle) => mSession?.HasCachedChildren(pMailboxHandle);

        /// <summary>
        /// Gets a report on the number of subscriptions to the events of this instance.
        /// </summary>
        /// <remarks>
        /// This report is provided to aid in the debugging of external code.
        /// </remarks>
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
