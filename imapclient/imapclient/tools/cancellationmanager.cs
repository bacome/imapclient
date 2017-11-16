using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Provides services for managing sets of <see langword="async"/> operations. 
    /// </summary>
    /// <remarks>
    /// Instances manage a series of internal <see cref="CancellationTokenSource"/> instances.
    /// Each time the internal <see cref="CancellationTokenSource"/> is cancelled (by using <see cref="Cancel(cTrace.cContext)"/>) a new <see cref="CancellationTokenSource"/> is allocated.
    /// Access to a <see cref="CancellationToken"/> attached to the current <see cref="CancellationTokenSource"/> is gained by calling <see cref="GetToken(cTrace.cContext)"/>.
    /// The objects issued by <see cref="GetToken(cTrace.cContext)"/> should be disposed when the <see langword="async"/> operation(s) being controlled by the contained <see cref="CancellationToken"/> finish
    /// to allow the <see cref="cCancellationManager"/> to manage the internal <see cref="CancellationTokenSource"/> instances better and to keep the <see cref="Count"/> property up to date.
    /// Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.
    /// </remarks>
    public sealed class cCancellationManager : IDisposable
    {
        private bool mDisposed = false;

        private readonly Action<cTrace.cContext> mCountChanged;
        private readonly object mCurrentCancellationSetLock = new object();
        private cCancellationSet mCurrentCancellationSet;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public cCancellationManager()
        {
            mCountChanged = null;
            mCurrentCancellationSet = new cCancellationSet(null);
        }

        /// <summary>
        /// Initialises a new instance specifying a callback to be used when the <see cref="Count"/> property changes.
        /// </summary>
        /// <param name="pCountChanged"></param>
        public cCancellationManager(Action<cTrace.cContext> pCountChanged)
        {
            mCountChanged = pCountChanged;
            mCurrentCancellationSet = new cCancellationSet(mCountChanged);
        }

        /// <summary>
        /// Issues a disposable token object containing a <see cref="CancellationToken"/> that is attached to the current internal <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns></returns>
        /// <remarks>
        /// Issuing the token object increments <see cref="Count"/>.
        /// Use the <see cref="CancellationToken"/> wrapped by the token object to control <see langword="async"/> operation(s).
        /// Dispose the token object when the operation(s) complete.
        /// Disposing the token object 'returns' the token and decrements <see cref="Count"/>.
        /// </remarks>
        public cToken GetToken(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCancellationManager), nameof(GetToken));

            if (mDisposed) throw new ObjectDisposedException(nameof(cCancellationManager));

            lock (mCurrentCancellationSetLock)
            {
                return mCurrentCancellationSet.GetToken(lContext);
            }
        }

        /// <summary>
        /// Gets the number of token objects currently issued.
        /// </summary>
        public int Count
        {
            get
            {
                if (mDisposed) throw new ObjectDisposedException(nameof(cCancellationManager));

                lock (mCurrentCancellationSetLock)
                {
                    return mCurrentCancellationSet.Count;
                }
            }
        }

        /// <summary>
        /// Cancels all of the operations attached to the current <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <remarks>
        /// If there are no token objects issued when this method is called, the method does nothing.
        /// If there are token objects issued, the internal <see cref="CancellationTokenSource"/> is cancelled and 
        /// a new internal <see cref="CancellationTokenSource"/> is allocated. A new set of operations can be started immediately.
        /// </remarks>
        public void Cancel(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCancellationManager), nameof(Cancel));

            if (mDisposed) throw new ObjectDisposedException(nameof(cCancellationManager));

            lock (mCurrentCancellationSetLock)
            {
                if (mCurrentCancellationSet.Count == 0) return;
                mCurrentCancellationSet.Cancel();
                mCurrentCancellationSet = new cCancellationSet(mCountChanged);
                mCountChanged?.Invoke(lContext);
            }
        }

        public void Dispose()
        {
            if (mDisposed) return;

            if (mCurrentCancellationSet != null)
            {
                try { mCurrentCancellationSet.Dispose(); }
                catch { }
            }

            mDisposed = true;
        }

        private sealed class cCancellationSet : IDisposable
        {
            private bool mDisposed = false;

            private Action<cTrace.cContext> mCountChanged;
            private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
            private int mCount = 0;

            public cCancellationSet(Action<cTrace.cContext> pCountChanged)
            {
                mCountChanged = pCountChanged; // can be null
            }

            public cToken GetToken(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCancellationSet), nameof(GetToken));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCancellationSet));
                Interlocked.Increment(ref mCount);
                mCountChanged?.Invoke(lContext);
                return new cToken(mCancellationTokenSource.Token, ZReleaseToken, pParentContext);
            }

            public int Count => mCount;

            public void Cancel()
            {
                mCountChanged = null;
                mCancellationTokenSource.Cancel();
                if (mCount == 0) Dispose();
            }

            private void ZReleaseToken(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCancellationSet), nameof(ZReleaseToken));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCancellationSet));
                if (Interlocked.Decrement(ref mCount) == 0 && mCancellationTokenSource.IsCancellationRequested) Dispose();
                else mCountChanged?.Invoke(pParentContext);
            }

            public void Dispose()
            {
                if (mDisposed) return;

                if (mCancellationTokenSource != null)
                {
                    try { mCancellationTokenSource.Dispose(); }
                    catch { }
                }

                mDisposed = true;
            }
        }

        /// <summary>
        /// Contains a <see cref="System.Threading.CancellationToken"/> attached to the internal <see cref="CancellationTokenSource"/> of a <see cref="cCancellationManager"/>.
        /// </summary>
        /// <remarks>
        /// Use the contained <see cref="System.Threading.CancellationToken"/> to control <see langword="async"/> operation(s).
        /// Dispose instances of this class when the operation(s) being controlled complete.
        /// Disposing the instances decrements <see cref="Count"/>.
        /// </remarks>
        public sealed class cToken : IDisposable
        {
            private bool mDisposed = false;

            /// <summary>
            /// The cancellation token to use to control the target <see langword="async"/> operation.
            /// </summary>
            public readonly CancellationToken CancellationToken;

            private readonly Action<cTrace.cContext> mReleaseToken;
            private readonly cTrace.cContext mContextToUseWhenDisposing;

            internal cToken(CancellationToken pCancellationToken, Action<cTrace.cContext> pReleaseToken, cTrace.cContext pContextToUseWhenDisposing)
            {
                CancellationToken = pCancellationToken;
                mReleaseToken = pReleaseToken ?? throw new ArgumentNullException(nameof(pReleaseToken));
                mContextToUseWhenDisposing = pContextToUseWhenDisposing ?? throw new ArgumentNullException(nameof(pContextToUseWhenDisposing));
            }

            public void Dispose()
            {
                if (mDisposed) return;

                try { mReleaseToken(mContextToUseWhenDisposing); }
                catch { }

                mDisposed = true;
            }
        }
    }
}