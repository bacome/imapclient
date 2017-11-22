using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Provides services for managing sets of <see cref="CancellationToken"/>. 
    /// </summary>
    /// <remarks>
    /// Instances manage a series of <see cref="CancellationTokenSource"/> instances.
    /// This class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.
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
        /// Initialises a new instance with the specified callback to be used when <see cref="Count"/> changes.
        /// </summary>
        /// <param name="pCountChanged"></param>
        public cCancellationManager(Action<cTrace.cContext> pCountChanged)
        {
            mCountChanged = pCountChanged;
            mCurrentCancellationSet = new cCancellationSet(mCountChanged);
        }

        /// <summary>
        /// Issues a disposable token object containing a <see cref="CancellationToken"/>.
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
        /// Cancels the current set of <see cref="CancellationToken"/> (if there are any token objects on issue) and starts a new set.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <remarks>
        /// The <see cref="Count"/> is reset to zero. 
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
        /// Contains a <see cref="System.Threading.CancellationToken"/> that can be used to control <see langword="async"/> operation(s).
        /// </summary>
        /// <remarks>
        /// Dispose instances of this class when the operation(s) being controlled complete.
        /// </remarks>
        public sealed class cToken : IDisposable
        {
            private bool mDisposed = false;

            /// <summary>
            /// The cancellation token.
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