using System;
using System.Diagnostics;
using System.Threading;
using work.bacome.trace;

namespace work.bacome.async
{
    public sealed class cAsyncOperationControl : IDisposable
    {
        public event Action<cTrace.cContext> CountChanged;

        private bool mDisposed = false;
        private readonly long mTimeout;
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private int mCount = 0;

        public cAsyncOperationControl(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            mTimeout = pTimeout;
        }

        public cToken GetToken(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cAsyncOperationControl), nameof(GetToken));
            if (mDisposed) throw new ObjectDisposedException(nameof(cAsyncOperationControl));
            Interlocked.Increment(ref mCount);
            return new cToken(mTimeout, mCancellationTokenSource.Token, ZReleaseToken, pParentContext);
        }

        public int Count => mCount;
        public void Cancel() => mCancellationTokenSource.Cancel();
        public bool IsCancellationRequested => mCancellationTokenSource.IsCancellationRequested;

        private void ZReleaseToken(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cAsyncOperationControl), nameof(ZReleaseToken));
            if (mDisposed) throw new ObjectDisposedException(nameof(cAsyncOperationControl));
            if (Interlocked.Decrement(ref mCount) == 0 && mCancellationTokenSource.IsCancellationRequested) Dispose();
        }

        public void Dispose()
        {
            if (mDisposed) return;

            if (mCancellationTokenSource != null)
            {
                try { mCancellationTokenSource.Dispose(); }
                catch { }
            }
        }

        public sealed class cToken : IDisposable
        {
            private bool mDisposed = false;
            private readonly long mTimeout;
            private readonly Stopwatch mStopwatch;
            private readonly CancellationToken mCancellationToken;
            private readonly Action<cTrace.cContext> mReleaseToken;
            private readonly cTrace.cContext mContextToUseWhenDisposing;

            public cToken(long pTimeout, CancellationToken pCancellationToken, Action<cTrace.cContext> pReleaseToken, cTrace.cContext pContextToUseWhenDisposing)
            {
                if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
                mTimeout = pTimeout;
                if (mTimeout == -1) mStopwatch = null;
                else mStopwatch = Stopwatch.StartNew();
                mCancellationToken = pCancellationToken;
                mReleaseToken = pReleaseToken;
                mContextToUseWhenDisposing = pContextToUseWhenDisposing;
            }

            public int Timeout
            {
                get
                {
                    if (mStopwatch == null) return System.Threading.Timeout.Infinite;
                    long lElapsed = mStopwatch.ElapsedMilliseconds;
                    if (mTimeout > lElapsed) return (int)(mTimeout - lElapsed);
                    return 0;
                }
            }

            public CancellationToken CancellationToken => mCancellationToken;

            public void Dispose()
            {
                if (mDisposed) return;

                try { mReleaseToken(mContextToUseWhenDisposing); }
                catch { }

                mDisposed = true;
            }

            public override string ToString()
            {
                if (mStopwatch == null) return $"{nameof(cToken)}({mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
                return $"{nameof(cToken)}({mStopwatch.ElapsedMilliseconds}/{mTimeout},{mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
            }
        }

    }
}