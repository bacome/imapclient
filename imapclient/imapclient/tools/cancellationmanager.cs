using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Instances manage sets of asynchronous operations that are attached to a common internal <see cref="CancellationTokenSource"/>. 
    /// </summary>
    /// <remarks>
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
        /// <param name="pCountChanged">The callback to be used when the <see cref="Count"/> property changes.</param>
        public cCancellationManager(Action<cTrace.cContext> pCountChanged)
        {
            mCountChanged = pCountChanged;
            mCurrentCancellationSet = new cCancellationSet(mCountChanged);
        }

        /// <summary>
        /// Gets a disposable object containing a <see cref="CancellationToken"/> that is attached to the current <see cref="CancellationTokenSource"/>. Increments <see cref="Count"/>.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns></returns>
        /// <remarks>
        /// Getting the token object increments <see cref="Count"/>.
        /// Dispose the returned object when the operation being controlled by the contained <see cref="CancellationToken"/> completes.
        /// Disposing the token object decrements <see cref="Count"/>.
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
        /// Gets the number of operations attached to the current <see cref="CancellationTokenSource"/>.
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
        /// Calling this method also causes the allocation of a new internal <see cref="CancellationTokenSource"/> so a new set of operations can be started immediately.
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
        /// Contains a <see cref="System.Threading.CancellationToken"/> attached to the <see cref="CancellationTokenSource"/> of a <see cref="cCancellationManager"/> instance.
        /// Dispose instances of this class when the operation being controlled by the contained <see cref="System.Threading.CancellationToken"/> completes.
        /// </summary>
        /// <remarks>
        /// Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.
        /// Disposing the instances decrements <see cref="cCancellationManager.Count"/>.
        /// </remarks>
        public sealed class cToken : IDisposable
        {
            private bool mDisposed = false;

            /// <summary>
            /// The cancellation token to use in the controlled operation.
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