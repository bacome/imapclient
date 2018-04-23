using System;
using System.Diagnostics;
using System.Threading;

namespace work.bacome.mailclient
{
    internal class cMethodControl
    {
        //public static readonly cMethodControl None = new cMethodControl(-1, CancellationToken.None);

        private readonly long mTimeout;
        private readonly Stopwatch mStopwatch;
        public readonly CancellationToken CancellationToken;

        public cMethodControl(int pTimeout, CancellationToken pCancellationToken)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            mTimeout = pTimeout;
            if (pTimeout == -1) mStopwatch = null;
            else mStopwatch = Stopwatch.StartNew();
            CancellationToken = pCancellationToken;
        }

        public int Timeout
        {
            get
            {
                if (mStopwatch == null) return System.Threading.Timeout.Infinite;
                long lElapsed = mStopwatch.ElapsedMilliseconds;
                if (mTimeout > lElapsed) return (int)(mTimeout - lElapsed);
                throw new TimeoutException();
            }
        }

        public override string ToString()
        {
            if (mStopwatch == null) return $"{nameof(cMethodControl)}({CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled})";
            return $"{nameof(cMethodControl)}({mStopwatch.ElapsedMilliseconds}/{mTimeout},{CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled})";
        }
    }
}