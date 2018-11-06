using System;
using System.Text;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

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
    public enum eIMAPConnectionState
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
    /// Before calling <see cref="Connect"/> set <see cref="Server"/> and <see cref="Authentication"/> at a minimum.
    /// Also consider setting <see cref="MailboxCacheDataItems"/>.
    /// </para>
    /// <para>This class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// </remarks>
    /// 

    public sealed partial class cIMAPClient : cMailClient 
    {
        // code checks
        //  check all awaits use configureawait(false)  quick and dirty: search for: await (?!.*\.ConfigureAwait\(false\).*\r?$) (this misses awaits with awaited parameters)   or this await [^(]*?\([^)]*\)[^.]

        // notes: (so I don't forget why)
        //
        //  the pipeline access system exists so that messages can be resolved to MSNs and to make select safe
        //   the problem is that if a command (other than FETCH, STORE, SEARCH - msnsafe commands) is in progress the server is allowed to send expunges: these invalidate message sequence numbers, 
        //    so the resolution of a message to its MSN has to take place while no msnUNsafe command is in progress and the resolved numbers have to be sent to the server before any msnUNsafe command is run
        //   we also don't want expunges or fetches arriving whilst selecting (do they apply to the old mailbox or the new one?)
        //    (note that this problem is mitigated by the rfc 7162 [CLOSED] response code)
        //  another problem is that if a command is in progress a UIDValidityChange can be sent by the server
        //   if we subsequently send (and note that this includes cases where the messages cross paths on the wire) an MSN or UID to the server, it may refer to the wrong message
        //   [note that this problem implies absolute single threading when dealing with message numbers,
        //     however rfc 3501 does not consider this possibility and explicitly encourages pipelining commands that expose the client to this problem,
        //     therefore I shall ignore this particular problem]
        //  the command completion hook exists so that returned MSNs can be resolved to messages, and to release locks held whilst commands are in progress
        //   MSN resolution has to be done at command completion because a subsequent command may send an expunge, invalidating the message numbers
        //   locks can't be released in the caller because the caller may time out AFTER the command is submitted (and the lock needs to be held until the server completes the command)
        //  the select lock is to make sure that the currently selected mailbox can be checked safely and to single thread select operations (so that the state on the client side is the same as the state on the server side)

        // notes on MDNSent
        //
        //  to implement MDNSent I need to not just recognise the MDNSent flag but also the fact that an MDN is required
        //   this involves getting and parsing the following headers;
        //    Disposition-Notification-To, Original-Recipient and Disposition-Notification-Options (see rfc 8098)
        //   the result of the parsing would be presented in an additional message attribute called MDNRequest which would be null if there are no headers
        //    or there are errors (like duplicate headers)
        //   so at this stage the MDNSent features are commented out as they aren't useful by themselves

        private static readonly cPersistentCache kDefaultPersistentCache = new cPersistentCache("default persistent cache", null, null, null);
        public static cPersistentCache GlobalPersistentCache { get; set; } = null;

        // mechanics
        private readonly cIMAPCallbackSynchroniser mIMAPSynchroniser;

        // property backing storage
        private Encoding mEncoding = Encoding.UTF8;
        private fIMAPCapabilities mIgnoreCapabilities = 0;
        private cIMAPAuthentication mAuthentication = null;
        private bool mMailboxReferrals = false;
        private fMailboxCacheDataItems mMailboxCacheDataItems = fMailboxCacheDataItems.messagecount | fMailboxCacheDataItems.uidvalidity | fMailboxCacheDataItems.uidnextcomponent | fMailboxCacheDataItems.unseencount;
        private cIdleConfiguration mIdleConfiguration = new cIdleConfiguration();
        private cPersistentCache _PersistentCache = null;
        private cBatchSizerConfiguration mSynchroniseCacheConfiguration = new cBatchSizerConfiguration(1, 1000, 10000, 1);
        private cBatchSizerConfiguration mFetchCacheItemsConfiguration = new cBatchSizerConfiguration(1, 1000, 10000, 1);
        private cBatchSizerConfiguration mFetchBodyConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private cBatchSizerConfiguration mAppendBatchConfiguration = new cBatchSizerConfiguration(1000, int.MaxValue, 10000, 1000);
        private cIMAPClientId mClientId = new cIMAPClientId(new cIMAPIdDictionary(true));
        private cIMAPClientIdUTF8 mClientIdUTF8 = null;
        private int mMaxItemsInSequenceSet = 50;

        // current session
        private cSession mSession = null;
        private cNamespaces mNamespaces = null; // if namespace is not supported by the server then this is used
        private cMailbox mInbox = null;
        private cBatchSizer mSynchroniseCacheSizer = null;
        private cBatchSizer mFetchCacheItemsSizer = null;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pInstanceName">The instance name to use for the instance's <see cref="cTrace"/> root-context.</param>
        public cIMAPClient(string pInstanceName = "work.bacome.cIMAPClient") : base(pInstanceName, new cIMAPCallbackSynchroniser())
        {
            var lContext = RootContext.NewObject(nameof(cIMAPClient), pInstanceName);
            mIMAPSynchroniser = (cIMAPCallbackSynchroniser)mSynchroniser;
        }

        public override fMessageDataFormat SupportedFormats => mSession?.SupportedFormats ?? 0;

        /// <summary>
        /// Gets and sets the encoding to use when <see cref="fEnableableExtensions.utf8"/> is not enabled.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="Encoding.UTF8"/>.
        /// Used when filtering by message content and when no encoding is specified when calling <see cref="cMailClient.GetHeaderFieldFactory(Encoding)"/>.
        /// When filtering, if the connected server does not support the encoding it will reject filters that use it and the library will throw <see cref="cUnsuccessfulIMAPCommandException"/> with <see cref="eIMAPResponseTextCode.badcharset"/>.
        /// </remarks>
        public override Encoding Encoding
        {
            get => mEncoding;

            set
            {
                var lContext = RootContext.NewSetProp(nameof(cIMAPClient), nameof(Encoding), value);
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!cCommandPartFactory.TryAsCharsetName(value, out _)) throw new ArgumentOutOfRangeException();
                mEncoding = value;
                mSession?.SetEncoding(value, lContext);
            }
        }

        /// <summary>
        /// Indicates whether the instance is currently unconnected.
        /// </summary>
        /// <seealso cref="IsConnected"/>
        /// <seealso cref="ConnectionState"/>
        public override bool IsUnconnected => mSession == null || mSession.IsUnconnected;

        /// <summary>
        /// Indicates whether the instance is currently connected.
        /// </summary>
        /// <seealso cref="IsUnconnected"/>
        /// <seealso cref="ConnectionState"/>
        public override bool IsConnected => mSession != null && mSession.IsConnected;

        public override cAccountId ConnectedAccountId => mSession?.ConnectedAccountId;

        /// <summary>
        /// Fired when server response text is received.
        /// </summary>
        /// <remarks>
        /// <para>The IMAP spec says that <see cref="eIMAPResponseTextCode.alert"/> text MUST be brought to the user's attention. See <see cref="cIMAPResponseTextEventArgs.Text"/>.</para>
        /// <para>
        /// If <see cref="SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler then the <see cref="CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </para>
        /// </remarks>
        public event EventHandler<cIMAPResponseTextEventArgs> ResponseText
        {
            add { mIMAPSynchroniser.ResponseText += value; }
            remove { mIMAPSynchroniser.ResponseText -= value; }
        }

        /// <summary>
        /// Fired when the server notifies the client of a change that could affect a property value of a <see cref="cMailbox"/> instance.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged
        {
            add { mIMAPSynchroniser.MailboxPropertyChanged += value; }
            remove { mIMAPSynchroniser.MailboxPropertyChanged -= value; }
        }

        /// <summary>
        /// Fired when the server notifies the client that messages have arrived in a mailbox.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery
        {
            add { mIMAPSynchroniser.MailboxMessageDelivery += value; }
            remove { mIMAPSynchroniser.MailboxMessageDelivery -= value; }
        }

        /// <summary>
        /// Fired when the server notifies the client of a change that could affect a property value of a <see cref="cIMAPMessage"/> instance.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged
        {
            add { mIMAPSynchroniser.MessagePropertyChanged += value; }
            remove { mIMAPSynchroniser.MessagePropertyChanged -= value; }
        }

        /// <summary>
        /// Sets <see cref="ServiceId"/>, defaulting the port to 143 and SSL to <see langword="false"/>. 
        /// </summary>
        /// <param name="pHost"></param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// </remarks>
        public void SetServiceId(string pHost) => base.ServiceId = new cServiceId(pHost, 143, false);

        /// <summary>
        /// Sets <see cref="ServiceId"/>, defaulting the port to 143 (no SSL) or 993 otherwise.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pSSL">Indicates whether the service requires that TLS be established immediately upon connect.</param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// </remarks>
        public void SetServiceId(string pHost, bool pSSL)
        {
            int lPort;
            if (pSSL) lPort = 993; else lPort = 143;
            base.ServiceId = new cServiceId(pHost, lPort, pSSL);
        }

        /// <summary>
        /// Sets <see cref="ServiceId"/>.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pPort"></param>
        /// <param name="pSSL">Indicates whether the service requires that TLS be established immediately upon connect.</param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// </remarks>
        public void SetServiceId(string pHost, int pPort, bool pSSL) => base.ServiceId = new cServiceId(pHost, pPort, pSSL);

        /// <summary>
        /// Gets and sets the server capabilities that the instance should ignore.
        /// </summary>
        /// <remarks>
        /// May only be set while <see cref="IsUnconnected"/>.
        /// Useful for testing or if your server (or the library) has a bug in its implementation of an IMAP extension.
        /// </remarks>
        /// <seealso cref="Capabilities"/>
        public fIMAPCapabilities IgnoreCapabilities
        {
            get => mIgnoreCapabilities;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                if ((value & fIMAPCapabilities.logindisabled) != 0) throw new ArgumentOutOfRangeException();
                mIgnoreCapabilities = value;
            }
        }

        /// <summary>
        /// Gets and sets the authentication details to be used by <see cref="Connect"/>.
        /// </summary>
        /// <remarks>
        /// Must be set before calling <see cref="Connect"/>. 
        /// May only be set while <see cref="IsUnconnected"/>.
        /// </remarks>
        /// <seealso cref="SetPlainAuthentication(string, string, eTLSRequirement, bool)"/>
        public cIMAPAuthentication Authentication
        {
            get => mAuthentication;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mAuthentication = value;
            }
        }

        /// <summary>
        /// Sets <see cref="Authentication"/> to use a userid and password combination to authenticate.
        /// </summary>
        /// <param name="pUserId"></param>
        /// <param name="pPassword"></param>
        /// <param name="pTLSRequirement">The TLS requirement for the userid and password to be used.</param>
        /// <param name="pTryAuthenticateEvenIfPlainIsntAdvertised">Indicates whether the SASL PLAIN mechanism should be tried even if it isn't advertised.</param>
        /// <remarks>
        /// May only be called while <see cref="IsUnconnected"/>.
        /// This method will throw if the userid and password can be used in neither <see cref="cIMAPLogin"/> nor <see cref="cSASLPlain"/>.
        /// </remarks>
        public void SetPlainAuthentication(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfPlainIsntAdvertised = false) => Authentication = cIMAPAuthentication.GetPlain(pUserId, pPassword, pTLSRequirement, pTryAuthenticateEvenIfPlainIsntAdvertised);

        // not tested yet
        //public void SetXOAuth2Credentials(string pUserId, string pAccessToken, bool pTryAuthenticateEvenIfXOAuth2IsntAdvertised = false) => Credentials = cCredentials.XOAuth2(pUserId, pAccessToken, pTryAuthenticateEvenIfXOAuth2IsntAdvertised);

        /// <summary>
        /// Gets and sets whether mailbox referrals will be handled.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="false"/>.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// If this is set to <see langword="false"/> the instance will not return remote mailboxes in mailbox lists.
        /// Handling mailbox referrals means handling the exceptions that could be raised when accessing remote mailboxes.
        /// See RFC 2193 for details.
        /// </remarks>
        /// <seealso cref="cUnsuccessfulIMAPCommandException"/>
        /// <seealso cref="cUnsuccessfulIMAPCommandException.ResponseText"/>
        /// <seealso cref="cIMAPResponseText.Arguments"/>
        /// <seealso cref="cURL"/>
        /// <seealso cref="cURI"/>
        public bool MailboxReferrals
        {
            get => mMailboxReferrals;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mMailboxReferrals = value;
            }
        }

        /// <summary>
        /// Gets and sets the set of optionally requested mailbox data items.
        /// </summary>
        /// <remarks>
        /// The default set is <see cref="fMailboxCacheDataItems.messagecount"/>, <see cref="fMailboxCacheDataItems.uidvalidity"/>, <see cref="fMailboxCacheDataItems.uidnextcomponent"/> and <see cref="fMailboxCacheDataItems.unseencount"/>.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// <note type="note" >
        /// The mailbox data items that are actually requested depends on the <see cref="fMailboxCacheDataSets"/> value used at the time of the request.
        /// </note>
        /// </remarks>
        public fMailboxCacheDataItems MailboxCacheDataItems
        {
            get => mMailboxCacheDataItems;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mMailboxCacheDataItems = value;
            }
        }

        /// <summary>
        /// Gets and sets the idle configuration. May be <see langword="null"/>.
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
                var lContext = RootContext.NewSetProp(nameof(cIMAPClient), nameof(IdleConfiguration), value);
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mIdleConfiguration = value;
                mSession?.SetIdleConfiguration(value, lContext);
            }
        }

        public cPersistentCache PersistentCache
        {
            get
            {
                var lSession = mSession;
                if (lSession == null || lSession.IsUnconnected) return _PersistentCache ?? GlobalPersistentCache ?? kDefaultPersistentCache;
                return lSession.PersistentCache;
            }

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                _PersistentCache = value;
            }
        }

        /// <summary>
        /// Gets and sets the synchronise-cache batch-size configuration. You might want to limit this to increase the speed with which you can cancel a select.
        /// </summary>
        /// <remarks>
        /// Limits the number of messages per batch when requesting flags from the server to synchronise the persistent cache at select time. Measured in number of messages.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// The default value is min=1 message, max=1000 messages, maxtime=10s, initial=1 message.
        /// </remarks>
        public cBatchSizerConfiguration SynchroniseCacheConfiguration
        {
            get => mSynchroniseCacheConfiguration;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mSynchroniseCacheConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// Gets and sets the fetch-cache-items batch-size configuration. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <remarks>
        /// Limits the number of messages per batch when requesting cache-items from the server. Measured in number of messages.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// The default value is min=1 message, max=1000 messages, maxtime=10s, initial=1 message.
        /// </remarks>
        public cBatchSizerConfiguration FetchCacheItemsConfiguration
        {
            get => mFetchCacheItemsConfiguration;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mFetchCacheItemsConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// Gets and sets the fetch-body-read batch-size configuration. You might want to limit this to increase the speed with which you can cancel the fetch.
        /// </summary>
        /// <remarks>
        /// Limits the size of the partial fetches used when getting body sections from the server. Measured in bytes.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// The default value is min=1000b, max=1000000b, maxtime=10s, initial=1000b.
        /// </remarks>
        public cBatchSizerConfiguration FetchBodyConfiguration
        {
            get => mFetchBodyConfiguration;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mFetchBodyConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// Gets and sets the append batch-size configuration. You might want to limit this to increase the speed with which you can cancel the append.
        /// </summary>
        /// <remarks>
        /// Limits the size of batches used when appending. Measured in bytes.
        /// If <see cref="cIMAPCapabilities.MultiAppend"/> is in use, limits the number of messages sent in a single append, otherwise limits the number of pipelined appends.
        /// May only be set while <see cref="IsUnconnected"/>.
        /// The default value is min=1000b, max=unlimited, maxtime=10s, initial=1000b.
        /// </remarks>
        public cBatchSizerConfiguration AppendBatchConfiguration
        {
            get => mAppendBatchConfiguration;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mAppendBatchConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// Gets and sets the ASCII ID (RFC 2971) details. 
        /// </summary>
        /// <remarks>
        /// If <see cref="cIMAPCapabilities.Id"/> is in use, these details are sent to the server during <see cref="Connect"/>.
        /// If <see cref="fEnableableExtensions.utf8"/> has been enabled and <see cref="ClientIdUTF8"/> is not <see langword="null"/>, then <see cref="ClientIdUTF8"/> will be used in preference to the value of this property.
        /// The default value is details about the library.
        /// Set this and <see cref="ClientIdUTF8"/> to <see langword="null"/> to send nothing to the server.
        /// </remarks>
        public cIMAPClientId ClientId
        {
            get => mClientId;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
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
        /// See <see cref="cIMAPClientId"/> and/ or <see cref="Connect"/> for more details.
        /// </remarks>
        public cIMAPClientIdUTF8 ClientIdUTF8
        {
            get => mClientIdUTF8 ?? mClientId;

            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mClientIdUTF8 = value;
            }
        }

        /// <summary>
        /// Gets and sets the maximum number of numbers that the library will use when generating an IMAP sequence-set to cover a set of numbers in contexts where specifying a wider range will not be harmful
        /// (e.g. in mailbox resynchronisation).
        /// </summary>
        /// <remarks>
        /// This value affects command line lengths in the places where it is used. Command line length may have a server-side limit. Any server-side limit is not discoverable by the library.
        /// The minimum value for this property is 4, the default value is 50.
        /// </remarks>
        public int MaxItemsInSequenceSet
        {
            get => mMaxItemsInSequenceSet;

            set
            {
                var lContext = RootContext.NewSetProp(nameof(cIMAPClient), nameof(MaxItemsInSequenceSet), value);
                if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (value < 4) throw new ArgumentOutOfRangeException();
                mMaxItemsInSequenceSet = value;
                mSession?.SetMaxItemsInSequenceSet(value, lContext);
            }
        }

        /// <summary>
        /// Gets the connection state of the instance.
        /// </summary>
        public eIMAPConnectionState ConnectionState => mSession?.ConnectionState ?? eIMAPConnectionState.notconnected;

        /// <summary>
        /// Gets the capabilities of the connected (or most recently connected) server. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The capabilities reflect the server capabilities less the <see cref="IgnoreCapabilities"/>.
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cIMAPCapabilities Capabilities => mSession?.Capabilities;

        /// <summary>
        /// Gets the extensions that the library has enabled on the connected (or most recently connected) server.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public fEnableableExtensions EnabledExtensions => mSession?.EnabledExtensions ?? fEnableableExtensions.none;

        public bool UTF8Enabled
        {
            get
            {
                var lSession = mSession;
                if (lSession == null) return false;
                return lSession.UTF8Enabled;
            }
        }

        /// <summary>
        /// Gets the login referral (RFC 2221), if received. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cURL HomeServerReferral => mSession?.HomeServerReferral;

        /// <summary>
        /// Gets whether the message sizes reported by IMAP are reliable.
        /// </summary>
        /// <remarks>
        /// The sizes may not be reliable if the server supports UTF8 and it is not enabled (see RFC 6858 section 3).
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public bool MessageSizesAreReliable
        {
            get
            {
                var lSession = mSession;
                if (lSession == null) return true;
                return lSession.MessageSizesAreReliable;
            }
        }

        /// <summary>
        /// Gets the ID (RFC 2971) details of the connected (or last connected) server, if they were sent. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
        /// </remarks>
        public cIMAPId ServerId => mSession?.ServerId;

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
        public cMailbox GetMailbox(cMailboxName pMailboxName)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            var lMailboxHandle = mSession.GetMailboxHandle(pMailboxName);

            return new cMailbox(this, lMailboxHandle);
        }

        /// <summary>
        /// Gets the namespace details for the connected (or last connected) account. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Set during <see cref="Connect"/>.
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

        internal object MailboxCache => mSession?.MailboxCache;
        internal iSelectedMailboxDetails SelectedMailboxDetails => mSession?.SelectedMailboxDetails;
        internal bool? HasCachedChildren(iMailboxHandle pMailboxHandle) => mSession?.HasCachedChildren(pMailboxHandle);

        public override string ToString() => $"{nameof(cIMAPClient)}({InstanceName})";
        
        protected override void Dispose(bool pDisposing)
        {
            if (IsDisposed) return;

            if (pDisposing)
            {
                if (mSession != null)
                {
                    try { mSession.Dispose(); }
                    catch { }
                }
            }

            base.Dispose(pDisposing);
        }
    }
}
