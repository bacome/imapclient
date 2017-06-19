using System;
using System.Threading;
using System.Diagnostics;

namespace work.bacome.async
{
    public class cMethodControl
    {
        private readonly long mTimeout;
        private readonly Stopwatch mStopwatch;
        private readonly CancellationToken mCancellationToken;

        public cMethodControl(int pTimeout, CancellationToken pCancellationToken)
        {
            mTimeout = pTimeout;
            if (mTimeout == System.Threading.Timeout.Infinite) mStopwatch = null;
            else mStopwatch = Stopwatch.StartNew();
            mCancellationToken = pCancellationToken;
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

        public override string ToString()
        {
            if (mStopwatch == null) return $"{nameof(cMethodControl)}({mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
            return $"{nameof(cMethodControl)}({mStopwatch.ElapsedMilliseconds}/{mTimeout},{mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
        }
    }
}