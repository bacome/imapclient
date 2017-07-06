using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
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
        public static Version Version = new Version(0, 1);
        public static DateTime ReleaseDate = new DateTime(2017, 6, 19);

        public enum eState { notconnected, connecting, notauthenticated, authenticated, selected, disconnecting, disconnected }

        public const string TraceSourceName = "work.bacome.cIMAPClient";
        private static readonly cTrace mTrace = new cTrace(TraceSourceName);

        // events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<cResponseTextEventArgs> ResponseText;
        public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged;
        public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery;
        public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged;

        // mechanics
        private bool mDisposed = false;
        private readonly cTrace.cContext mRootContext;
        private readonly cEventSynchroniser mEventSynchroniser;
        private readonly cAsyncCounter mAsyncCounter;

        // current session
        private cSession mSession = null;

        // property backing storage
        private int mTimeout = -1;
        private fCapabilities mIgnoreCapabilities = 0;
        private cServer mServer = null;
        private cCredentials mCredentials = null;
        private cIdleConfiguration mIdleConfiguration = new cIdleConfiguration();
        private cFetchSizeConfiguration mFetchAttributesConfiguration = new cFetchSizeConfiguration(1, 1000, 10000, 1);
        private cFetchSizeConfiguration mFetchBodyReadConfiguration = new cFetchSizeConfiguration(1000, 1000000, 10000, 1000);
        private cFetchSizeConfiguration mFetchBodyWriteConfiguration = new cFetchSizeConfiguration(1000, 1000000, 10000, 1000);
        private Encoding mEncoding = Encoding.UTF8;
        private cId mClientId = new cId(new cIdReadOnlyDictionary(cIdDictionary.CreateDefaultClientIdDictionary()));

        public cIMAPClient(string pInstanceName = TraceSourceName)
        {
            mRootContext = mTrace.NewRoot(pInstanceName);
            mRootContext.TraceInformation("cIMAPClient by bacome version {0}, release date {1}", Version, ReleaseDate);
            mEventSynchroniser = new cEventSynchroniser(this, mRootContext);
            mAsyncCounter = new cAsyncCounter(mEventSynchroniser.PropertyChanged);
        }

        // the synchronisation context on which the events should be delivered
        //  if null, any context will do
        //   (changing this while events are being delivered will result in undefined behaviour)
        //
        public SynchronizationContext SynchronizationContext
        {
            get => mEventSynchroniser.SynchronizationContext;
            set => mEventSynchroniser.SynchronizationContext = value;
        }

        public int Timeout
        {
            get => mTimeout;

            set
            {
                if (value < -1) throw new ArgumentOutOfRangeException();
                mTimeout = value;
            }
        }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        // state of the connection
        public eState State => mSession?.State ?? eState.notconnected;
        public cCapability Capability => mSession?.Capability;
        public fEnableableExtensions EnabledExtensions => mSession?.EnabledExtensions ?? fEnableableExtensions.none;
        public cAccountId ConnectedAccountId => mSession?.ConnectedAccountId;

        // the login referral (rfc 2221) if received
        public cURL HomeServerReferral => mSession?.HomeServerReferral;

        // for cheapskate UIs: if this number is greater than zero then a cancel button might be enabled
        //   (to cancel the cancellationtokensource associated with the cancellationtoken that has been set via the property above)
        //
        public int AsyncCount => mAsyncCounter.Count;

        // list of capabilities to ignore
        public fCapabilities IgnoreCapabilities
        {
            get => mIgnoreCapabilities;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(IgnoreCapabilities), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mIgnoreCapabilities = value;
                mSession?.SetIgnoreCapabilities(mIgnoreCapabilities, lContext);
            }
        } 

        // the server that the client should connect to

        public cServer Server
        {
            get => mServer;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (State != eState.notconnected && State != eState.disconnected) throw new InvalidOperationException();
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
                if (State != eState.notconnected && State != eState.disconnected) throw new InvalidOperationException();
                mCredentials = value;
            }
        }

        public void SetNoCredentials() => Credentials = cCredentials.None;
        public void SetAnonymousCredentials(string pTrace, bool pTryAuthenticateEvenIfAuthAnonymousIsntAdvertised = false) => Credentials = cCredentials.Anonymous(pTrace, pTryAuthenticateEvenIfAuthAnonymousIsntAdvertised);
        public void SetPlainCredentials(string pUserId, string pPassword, bool pTryAuthenticateEvenIfAuthPlainIsntAdvertised = false) => Credentials = cCredentials.Plain(pUserId, pPassword, pTryAuthenticateEvenIfAuthPlainIsntAdvertised);

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

        public cFetchSizeConfiguration FetchAttributesConfiguration
        {
            get => mFetchAttributesConfiguration;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(FetchAttributesConfiguration), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mFetchAttributesConfiguration = value ?? throw new ArgumentNullException();
                mSession?.SetFetchAttributesConfiguration(value, lContext);
            }
        }

        public cFetchSizeConfiguration FetchBodyReadConfiguration
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

        public cFetchSizeConfiguration FetchBodyWriteConfiguration
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
        public Encoding Encoding
        {
            get => mEncoding;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(Encoding), value.WebName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (!cCommandPart.TryAsCharsetName(value.WebName, out _)) throw new ArgumentOutOfRangeException();
                mEncoding = value;
                mSession?.SetEncoding(value, lContext);
            }
        }

        // id command (rfc 2971): a way of identifying the client and server software versions

        public cId ClientId
        {
            get => mClientId;

            set
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (State != eState.notconnected && State != eState.disconnected) throw new InvalidOperationException();
                mClientId = value;
            }
        }

        public cIdReadOnlyDictionary ServerId => mSession?.ServerId;

        // rfc 2342 namespaces (if the server doesn't support namespaces then this will still work - there will be one personal namespace retrieved using LIST)
        public cNamespaces Namespaces
        {
            get
            {
                if (mSession == null) return null;
                if (mSession.ConnectedAccountId == null) return null;
                return new cNamespaces(this, mSession.ConnectedAccountId, mSession.PersonalNamespaces, mSession.OtherUsersNamespaces, mSession.SharedNamespaces);
            }
        }

        public cMailbox Inbox => mSession?.Inbox;
        public cMailboxId SelectedMailboxId => mSession?.SelectedMailboxId;
        public iMailboxCacheItem MailboxCacheItem(cMailboxId pMailboxId) => mSession?.MailboxCacheItem(pMailboxId);

        public void Dispose()
        {
            if (mDisposed) return;

            if (mSession != null)
            {
                try { mSession.Dispose(); }
                catch { }
            }

            if (mEventSynchroniser != null)
            {
                try { mEventSynchroniser.Dispose(); }
                catch { }
            }

            mDisposed = true;
        }
    }
}
