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

        // version and release date of this class
        public static Version Version = new Version(0, 2);
        public static DateTime ReleaseDate = new DateTime(2017, 7, 18);

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
        private cIdleConfiguration mIdleConfiguration = new cIdleConfiguration();
        private cBatchSizerConfiguration mFetchCacheItemsConfiguration = new cBatchSizerConfiguration(1, 1000, 10000, 1);
        private cBatchSizerConfiguration mFetchBodyReadConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private cBatchSizerConfiguration mFetchBodyWriteConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private Encoding mEncoding = Encoding.UTF8;
        private cClientId mClientId = new cClientId(new cIdDictionary(true));
        private cClientIdUTF8 mClientIdUTF8 = null;

        public cIMAPClient(string pInstanceName = TraceSourceName)
        {
            mInstanceName = pInstanceName;
            mRootContext = mTrace.NewRoot(pInstanceName);
            mRootContext.TraceInformation("cIMAPClient by bacome version {0}, release date {1}", Version, ReleaseDate);
            mSynchroniser = new cCallbackSynchroniser(this, mRootContext);
            mCancellationManager = new cCancellationManager(mSynchroniser.InvokeCancellableCountChanged);
        }

        public string InstanceName => mInstanceName;

        // events

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

        public event EventHandler<cResponseTextEventArgs> ResponseText
        {
            add { mSynchroniser.ResponseText += value; }
            remove { mSynchroniser.ResponseText -= value; }
        }

        public event EventHandler<cNetworkActivityEventArgs> NetworkActivity
        {
            add { mSynchroniser.NetworkActivity += value; }
            remove { mSynchroniser.NetworkActivity -= value; }
        }

        public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged
        {
            add { mSynchroniser.MailboxPropertyChanged += value; }
            remove { mSynchroniser.MailboxPropertyChanged -= value; }
        }

        public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery
        {
            add { mSynchroniser.MailboxMessageDelivery += value; }
            remove { mSynchroniser.MailboxMessageDelivery -= value; }
        }

        public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged
        {
            add { mSynchroniser.MessagePropertyChanged += value; }
            remove { mSynchroniser.MessagePropertyChanged -= value; }
        }

        public event EventHandler<cCallbackExceptionEventArgs> CallbackException
        {
            add { mSynchroniser.CallbackException += value; }
            remove { mSynchroniser.CallbackException -= value; }
        }

        // async operation management

        public int Timeout
        {
            get => mTimeout;

            set
            {
                if (value < -1) throw new ArgumentOutOfRangeException();
                mTimeout = value;
            }
        }

        public int CancellableCount => mCancellationManager.Count;

        public void Cancel()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Cancel));
            mCancellationManager.Cancel(lContext);
        }

        // state

        public eConnectionState ConnectionState => mSession?.ConnectionState ?? eConnectionState.notconnected;
        public bool IsUnconnected => mSession == null || mSession.IsUnconnected;
        public bool IsConnected => mSession != null && mSession.IsConnected;

        public cCapabilities Capabilities => mSession?.Capabilities;
        public fEnableableExtensions EnabledExtensions => mSession?.EnabledExtensions ?? fEnableableExtensions.none;
        public cAccountId ConnectedAccountId => mSession?.ConnectedAccountId;

        // the login referral (rfc 2221) if received
        public cURL HomeServerReferral => mSession?.HomeServerReferral;

        // capabilities to ignore
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

        // the server that the client should connect to

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

        public void SetServer(string pHost) => Server = new cServer(pHost);
        public void SetServer(string pHost, bool pSSL) => Server = new cServer(pHost, pSSL);
        public void SetServer(string pHost, int pPort, bool pSSL) => Server = new cServer(pHost, pPort, pSSL);

        // the credentials that should be used to connect to the server

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

        public void SetNoCredentials() => Credentials = cCredentials.None;
        public void SetAnonymousCredentials(string pTrace, eTLSRequirement pTLSRequirement = eTLSRequirement.indifferent, bool pTryAuthenticateEvenIfAuthAnonymousIsntAdvertised = false) => Credentials = cCredentials.Anonymous(pTrace, pTLSRequirement, pTryAuthenticateEvenIfAuthAnonymousIsntAdvertised);
        public void SetPlainCredentials(string pUserId, string pPassword, eTLSRequirement pTLSRequirement = eTLSRequirement.required, bool pTryAuthenticateEvenIfAuthPlainIsntAdvertised = false) => Credentials = cCredentials.Plain(pUserId, pPassword, pTLSRequirement, pTryAuthenticateEvenIfAuthPlainIsntAdvertised);

        // if the caller can handle mailbox referrals
        //
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

        // the properties required when listing mailboxes
        //
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

        // idle config; either IDLE (rfc 2177) or base protocol CHECK and NOOP
        //  the command pipeline waits until a certain period of inactivity has occurred before starting either an IDLE command or periodic polling
        //
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

        // controls for long running activities

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

        // encoding to use when UTF8 (rfc 6855) is not supported directly by the server
        //
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

        // id command (rfc 2971)
        
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

        public cId ServerId => mSession?.ServerId;

        // rfc 2342 namespaces (if the server doesn't support namespaces then this will still work - there will be one personal namespace retrieved using LIST)
        //
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

        public cMailbox Inbox => mInbox;

        public iSelectedMailboxDetails SelectedMailboxDetails => mSession?.SelectedMailboxDetails;

        public cMailbox SelectedMailbox
        {
            get
            {
                var lDetails = mSession?.SelectedMailboxDetails;
                if (lDetails == null) return null;
                return new cMailbox(this, lDetails.Handle);
            }
        }

        public cMailbox Mailbox(cMailboxName pMailboxName)
        {
            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            var lHandle = mSession.GetMailboxHandle(pMailboxName);

            return new cMailbox(this, lHandle);
        }

        public bool? HasCachedChildren(iMailboxHandle pHandle) => mSession?.HasCachedChildren(pHandle);

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
