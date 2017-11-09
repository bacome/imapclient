using System;
using System.Diagnostics;
using System.Threading;

namespace work.bacome.async
{
    /// <summary>
    /// <para>Instances can be used to control the execution of an asynchronous method.</para>
    /// <para>Instances have a timeout and a <see cref="CancellationToken"/>.</para>
    /// <para>The timeout runs from when the instance is created; each time the value of the <see cref="Timeout"/> property is retrieved only the time remaining is returned. i.e. if the method being controlled has many internal async calls the timeout applies to the total time of all the internal calls.</para>
    /// <para>Infinite timeouts are supported (use -1 or <see cref="System.Threading.Timeout.Infinite"/>)</para>
    /// </summary>
    public class cMethodControl
    {
        private readonly long mTimeout;
        private readonly Stopwatch mStopwatch;
        private readonly CancellationToken mCancellationToken;

        public cMethodControl(int pTimeout, CancellationToken pCancellationToken)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            mTimeout = pTimeout;
            if (mTimeout == -1) mStopwatch = null;
            else mStopwatch = Stopwatch.StartNew();
            mCancellationToken = pCancellationToken;
        }

        /// <summary>
        /// The amount of time remaining (or <see cref="System.Threading.Timeout.Infinite"/> if there is no timeout for this instance).
        /// </summary>
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

        /// <summary>
        /// The <see cref="CancellationToken"/> to use.
        /// </summary>
        public CancellationToken CancellationToken => mCancellationToken;

        public override string ToString()
        {
            if (mStopwatch == null) return $"{nameof(cMethodControl)}({mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
            return $"{nameof(cMethodControl)}({mStopwatch.ElapsedMilliseconds}/{mTimeout},{mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
        }
    }
}