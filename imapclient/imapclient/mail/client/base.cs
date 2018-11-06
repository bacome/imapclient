using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract partial class cMailClient : IDisposable
    {
        // ......................................................................................................................... when changing the version here also change it in the assemblyinfo
        //  the convention for this is that it match the AssemblyInformationalVersion in the assemblyinfo.cs , with 
        //   revision matching the pre-release version: -alpha (1) -beta (2) -rc (3) 
        //   a general release would have no "-xxx" in the AssemblyInformationalVersion and should be x.y.0 where y is one greater than the y in the -rc version
        //    bug fixes to the general release would be x.y.z.0
        //
        //    0, 5, 2, 1 -> 0.5.2-alpha; 2017-NOV-30
        //
        private const int kAlpha = 1;
        private const int kBeta = 2;
        private const int kRC = 3;
        //
        /**<summary>The version number of the library..</summary>*/
        public static readonly Version Version = new Version(0, 6, 0, kAlpha);
        //
        // ......................................................................................................................... when changing the version here also change it in the assemblyinfo

        /**<summary>The release date of the library.</summary>*/
        public static readonly DateTime ReleaseDate = new DateTime(2017, 12, 02);

        // tracing
        internal static readonly cTrace Trace = new cTrace("work.bacome.cMailClient");

        // empty byte buffer
        internal static readonly byte[] ZeroLengthBuffer = new byte[0];

        // arbitrary size
        internal const int BufferSize = 4096;

        // mechanics
        private bool mDisposed = false;
        public readonly string InstanceName;
        protected readonly cCallbackSynchroniser mSynchroniser;
        protected internal readonly cCancellationManager CancellationManager;
        protected internal readonly cTrace.cContext RootContext;

        // property backing storage
        private int mTimeout = -1;
        private int mIncrementInvokeMillisecondsDelay = 100;
        private cServiceId mServiceId = null;
        private cBatchSizerConfiguration mNetworkWriteConfiguration = new cBatchSizerConfiguration(1000, 1000000, 10000, 1000);
        private ReadOnlyCollection<cSASLAuthentication> mFailedSASLAuthentications = null;

        protected cMailClient(string pInstanceName, cCallbackSynchroniser pSynchroniser)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));

            RootContext = Trace.NewRoot(pInstanceName);
            RootContext.TraceInformation("cMailClient by bacome version {0}, release date {1}", Version, ReleaseDate);

            CancellationManager = new cCancellationManager(pSynchroniser.InvokeCancellableCountChanged);

            mSynchroniser.Start(this, RootContext);
        }

        /// <summary>
        /// Gets the message formats that are supported by the instance.
        /// </summary>
        public abstract fMessageDataFormat SupportedFormats { get; }

        /// <summary>
        /// Gets and sets the default encoding to use.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="Encoding.UTF8"/>.
        /// Used when no encoding is specified when calling <see cref="GetHeaderFieldFactory(Encoding)"/>.
        /// </remarks>
        public abstract Encoding Encoding { get; set; }

        /// <summary>
        /// Indicates whether the instance is currently unconnected.
        /// </summary>
        public abstract bool IsUnconnected { get; }

        /// <summary>
        /// Indicates whether the instance is currently connected.
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Gets the accountid of the current (or most recent) connection. May be <see langword="null"/>.
        /// </summary>
        public abstract cAccountId ConnectedAccountId { get; }

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
        /// Fired when a server response is received.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cNetworkReceiveEventArgs> NetworkReceive
        {
            add { mSynchroniser.NetworkReceive += value; }
            remove { mSynchroniser.NetworkReceive -= value; }
        }

        /// <summary>
        /// Fired when the client sends a command.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cNetworkSendEventArgs> NetworkSend
        {
            add { mSynchroniser.NetworkSend += value; }
            remove { mSynchroniser.NetworkSend -= value; }
        }

        /// <summary>
        /// Fired when an exception is raised by external code in a callback or event handler.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event EventHandler<cCallbackExceptionEventArgs> CallbackException
        {
            add { mSynchroniser.CallbackException += value; }
            remove { mSynchroniser.CallbackException -= value; }
        }

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
        /// Gets and sets the timeout for calls where no operation specific value for a timeout can be (or has been) specified.
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
        /// Gets and sets the cancellation token for calls where no operation specific value for a cancellation token can be (or has been) specified. May be <see cref="CancellationToken.None"/>.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="CancellationToken.None"/>.
        /// If this is <see cref="CancellationToken.None"/> then the call will use an internal cancellation token instead, and active operations can be cancelled using <see cref="Cancel"/>.
        /// </remarks>
        public CancellationToken CancellationToken
        {
            get => CancellationManager.CancellationToken;
            set => CancellationManager.CancellationToken = value;
        }

        /// <summary>
        /// Gets the number of operations that will be cancelled by <see cref="Cancel"/>.
        /// </summary>
        public int CancellableCount => CancellationManager.Count;

        /// <summary>
        /// Cancels the operations that are using the internal cancellation token.
        /// </summary>
        public void Cancel()
        {
            var lContext = RootContext.NewMethod(nameof(cMailClient), nameof(Cancel));
            CancellationManager.Cancel(lContext);
        }

        /// <summary>
        /// Gets and sets the preferred frequency of invokes of progress-increment callbacks from the instance.
        /// </summary>
        /// <remarks>
        /// May only be set while <see cref="IsUnconnected"/>.
        /// </remarks>
        /// <seealso cref="cMethodConfiguration"/>
        public int IncrementInvokeMillisecondsDelay
        {
            get => mIncrementInvokeMillisecondsDelay;

            set
            {
                if (value < -1) throw new ArgumentOutOfRangeException();
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mIncrementInvokeMillisecondsDelay = value;
            }
        }

        /// <summary>
        /// Gets and sets the service to connect to. 
        /// </summary>
        /// <remarks>
        /// May only be set while <see cref="IsUnconnected"/>.
        /// </remarks>
        public cServiceId ServiceId
        {
            get => mServiceId;

            set
            {
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mServiceId = value;
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
                if (!IsUnconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                mNetworkWriteConfiguration = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// Gets the set of SASL authentication objects that failed during the last attempt to <see cref="Connect"/>. May be <see langword="null"/> or empty.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see langword="null"/> indicates that the instance has never tried to <see cref="Connect"/>.
        /// The collection will be empty if there were no failed SASL authentication attempts in the last attempt to <see cref="Connect"/>.
        /// </para>
        /// <para>
        /// This property is provided to give access to authentication error detail that is specific to the authentication mechanism.
        /// (For example: XOAUTH2 provides an 'Error Response' as part of a failed attempt to authenticate.)
        /// </para>
        /// <note type="note">
        /// All objects in this collection will have been disposed.
        /// </note>
        /// </remarks>
        public ReadOnlyCollection<cSASLAuthentication> FailedSASLAuthentications
        {
            get => mFailedSASLAuthentications;
            protected set => mFailedSASLAuthentications = value ?? throw new ArgumentNullException();
        }

        public cHeaderFieldFactory GetHeaderFieldFactory(Encoding pEncoding = null) => new cHeaderFieldFactory((SupportedFormats & fMessageDataFormat.utf8headers) != 0, pEncoding ?? Encoding);

        public bool IsDisposed => mDisposed;

        internal void Wait(Task pAsyncTask, cTrace.cContext pParentContext) => mSynchroniser.Wait(pAsyncTask, pParentContext);



        /*
        internal void InvokeActionInt(Action<int> pAction, int pInt, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailClient), nameof(InvokeActionInt), pInt);
            mSynchroniser.InvokeActionInt(pAction, pInt, lContext);
        } */

        internal void InvokeActionLong(Action<long> pAction, long pLong, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailClient), nameof(InvokeActionLong), pLong);
            mSynchroniser.InvokeActionLong(pAction, pLong, lContext);
        }

        internal iIncrementer GetNewIncrementer(Action<int> pIncrement, cTrace.cContext pContextForInvoke) => new cIncrementer(mSynchroniser, pIncrement, mIncrementInvokeMillisecondsDelay, pContextForInvoke);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool pDisposing)
        {
            if (mDisposed) return;

            if (pDisposing)
            {
                if (mSynchroniser != null)
                {
                    try { mSynchroniser.Dispose(); }
                    catch { }
                }

                if (CancellationManager != null)
                {
                    try { CancellationManager.Dispose(); }
                    catch { }
                }
            }

            mDisposed = true;
        }
    }
}
