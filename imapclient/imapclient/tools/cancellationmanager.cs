using System;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.async
{
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

        public cCancellationManager(Action<cTrace.cContext> pCountChanged)
        {
            mCountChanged = pCountChanged;
            mCurrentCancellationSet = new cCancellationSet(mCountChanged);
        }

        public cToken GetToken(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCancellationManager), nameof(GetToken));

            if (mDisposed) throw new ObjectDisposedException(nameof(cCancellationManager));

            lock (mCurrentCancellationSetLock)
            {
                return mCurrentCancellationSet.GetToken(lContext);
            }
        }

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

        public sealed class cToken : IDisposable
        {
            private bool mDisposed = false;
            public readonly CancellationToken CancellationToken;
            private readonly Action<cTrace.cContext> mReleaseToken;
            private readonly cTrace.cContext mContextToUseWhenDisposing;

            public cToken(CancellationToken pCancellationToken, Action<cTrace.cContext> pReleaseToken, cTrace.cContext pContextToUseWhenDisposing)
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