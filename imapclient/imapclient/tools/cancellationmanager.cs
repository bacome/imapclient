using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Manages sets (cancellation-sets) of concurrent asynchronous operations that are attached to a common <see cref="CancellationTokenSource"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="GetToken(cTrace.cContext)"/> to get a disposable object containing a <see cref="CancellationToken"/> that is attached to the current <see cref="CancellationTokenSource"/>.
    /// Dispose the object when the operation controlled by the contained <see cref="CancellationToken"/> completes.
    /// </para>
    /// <para>Use <see cref="Cancel(cTrace.cContext)"/> to cancel all of the operations currently underway; this causes the allocation of a new internal <see cref="CancellationTokenSource"/> so new operations can be started in a new cancellation-set immediately.</para>
    /// <para>Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// </remarks>
    public sealed class cCancellationManager : IDisposable
    {
        private bool mDisposed = false;

        private readonly Action<cTrace.cContext> mCountChanged;
        private readonly object mCurrentCancellationSetLock = new object();
        private cCancellationSet mCurrentCancellationSet;

        public cCancellationManager()
        {
            mCountChanged = null;
            mCurrentCancellationSet = new cCancellationSet(null);
        }

        /// <summary>
        /// Constructor specifying a callback to be used when <see cref="Count"/> changes.
        /// </summary>
        /// <param name="pCountChanged">A callback to be used when <see cref="Count"/> changes.</param>
        public cCancellationManager(Action<cTrace.cContext> pCountChanged)
        {
            mCountChanged = pCountChanged;
            mCurrentCancellationSet = new cCancellationSet(mCountChanged);
        }

        /// <summary>Returns a disposable object containing a <see cref="CancellationToken"/>. Dispose the object when the operation controlled by the contained <see cref="CancellationToken"/> completes.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns>A disposable object containing a <see cref="CancellationToken"/>.</returns>
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
        /// The number of active concurrent operations in the current cancellation-set.
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
        /// Cancel the active operations in the current cancellation-set.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
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
        /// Contains a <see cref="CancellationToken"/> attached to a <see cref="CancellationTokenSource"/> of a <see cref="cCancellationManager"/>. Note that the class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.
        /// </summary>
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