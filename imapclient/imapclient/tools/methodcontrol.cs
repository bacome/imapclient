using System;
using System.Diagnostics;
using System.Threading;

namespace work.bacome.async
{
    /// <summary>
    /// Instances represent controls on the execution of an asynchronous method. Instances have a timeout and a <see cref="CancellationToken"/>. 
    /// </summary>
    /// <remarks>
    /// Any timeout runs from when the instance is created; each time the value of the <see cref="Timeout"/> property is retrieved only the time remaining is returned. 
    /// So if the method being controlled has many internal async calls, the timeout applies to the total time of all the internal calls.
    /// Infinite timeouts are supported (use <see cref="System.Threading.Timeout.Infinite"/> for the timeout).
    /// </remarks>
    public class cMethodControl
    {
        private readonly long mTimeout;
        private readonly Stopwatch mStopwatch;
        private readonly CancellationToken mCancellationToken;

        /// <summary>
        /// Initialises a new instance with a timeout and a <see cref="CancellationToken"/>. 
        /// </summary>
        /// <param name="pTimeout">The timeout to use (use <see cref="System.Threading.Timeout.Infinite"/> for no timeout).</param>
        /// <param name="pCancellationToken">The cancellation token to use (use <see cref="System.Threading.CancellationToken.None"/> for no cancellation).</param>
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
        /// The <see cref="System.Threading.CancellationToken"/> being used.
        /// </summary>
        public CancellationToken CancellationToken => mCancellationToken;

        public override string ToString()
        {
            if (mStopwatch == null) return $"{nameof(cMethodControl)}({mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
            return $"{nameof(cMethodControl)}({mStopwatch.ElapsedMilliseconds}/{mTimeout},{mCancellationToken.IsCancellationRequested}/{mCancellationToken.CanBeCanceled})";
        }
    }
}