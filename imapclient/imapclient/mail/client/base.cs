using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
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

        // property backing storage
        private int mTimeout = -1;

        internal cMailClient() { }

        protected abstract cTrace.cContext YRootContext { get; }
        protected abstract cCallbackSynchroniser YSynchroniser { get; }
        protected abstract cCancellationManager YCancellationManager { get; }

        /// <summary>
        /// Gets the message formats that are supported by the instance.
        /// </summary>
        public abstract fMessageDataFormat SupportedFormats { get; }

        /// <summary>
        /// Gets and sets the encoding to use when <see cref="fMessageDataFormat.utf8headers"/> is not supported by the instance.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="Encoding.UTF8"/>.
        /// </remarks>
        public abstract Encoding Encoding { get; set; }

        /// <summary>
        /// Fired when a property value of the instance changes.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { YSynchroniser.PropertyChanged += value; }
            remove { YSynchroniser.PropertyChanged -= value; }
        }

        /// <summary>
        /// Fired when a server response is received.
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
            add { YSynchroniser.NetworkReceive += value; }
            remove { YSynchroniser.NetworkReceive -= value; }
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
            add { YSynchroniser.NetworkSend += value; }
            remove { YSynchroniser.NetworkSend -= value; }
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
            add { YSynchroniser.CallbackException += value; }
            remove { YSynchroniser.CallbackException -= value; }
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
            get => YSynchroniser.SynchronizationContext;
            set => YSynchroniser.SynchronizationContext = value;
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
        /// <seealso cref="CancellableCount"/>
        /// <seealso cref="Cancel"/>
        public CancellationToken CancellationToken
        {
            get => YCancellationManager.CancellationToken;
            set => YCancellationManager.CancellationToken = value;
        }

        /// <summary>
        /// Gets the number of operations that will be cancelled by <see cref="Cancel"/>.
        /// </summary>
        /// <seealso cref="CancellationToken"/>
        /// <seealso cref="Cancel"/>
        public int CancellableCount => YCancellationManager.Count;

        /// <summary>
        /// Cancels the operations that are using the internal cancellation token.
        /// </summary>
        /// <seealso cref="CancellationToken"/>
        /// <seealso cref="CancellableCount"/>
        public void Cancel()
        {
            var lContext = YRootContext.NewMethod(nameof(cMailClient), nameof(Cancel));
            YCancellationManager.Cancel(lContext);
        }

        public cHeaderFieldFactory HeaderFieldFactory(Encoding pEncoding = null) =>  new cHeaderFieldFactory((SupportedFormats & fMessageDataFormat.utf8headers) != 0, pEncoding ?? Encoding);

        internal void InvokeActionLong(Action<long> pAction, long pLong)
        {
            var lContext = YRootContext.NewMethod(nameof(cMailClient), nameof(InvokeActionLong), pLong);
            YSynchroniser.InvokeActionLong(pAction, pLong, lContext);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool pDisposing) { }
    }
}
