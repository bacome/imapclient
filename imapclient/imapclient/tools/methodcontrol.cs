using System;
using System.Diagnostics;
using System.Threading;
using work.bacome.apidocumentation;

namespace work.bacome.async
{
    /// <summary>
    /// Represents <see langword="async"/> method controls. 
    /// </summary>
    /// <remarks>
    /// Any timeout specified runs from when the instance is created; each time the value of <see cref="Timeout"/> is retrieved only the time remaining is returned. 
    /// This means that if the method being controlled itself makes <see langword="async"/> calls, the timeout can be applied to the total time that the method takes.
    /// </remarks>
    public class cMethodControl
    {
        private readonly long mTimeout;
        private readonly Stopwatch mStopwatch;
        /**<summary>The cancellation token being used.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// Initialises a new instance. 
        /// </summary>
        /// <param name="pTimeout">The timeout to use in milliseconds (or <see cref="System.Threading.Timeout.Infinite"/> for no timeout).</param>
        /// <param name="pCancellationToken">The cancellation token to use (or <see cref="System.Threading.CancellationToken.None"/> for no cancellation).</param>
        /// <inheritdoc cref="cMethodControl" select="remarks"/>
        public cMethodControl(int pTimeout, CancellationToken pCancellationToken)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            mTimeout = pTimeout;
            if (mTimeout == -1) mStopwatch = null;
            else mStopwatch = Stopwatch.StartNew();
            CancellationToken = pCancellationToken;
        }

        /// <summary>
        /// Gets the amount of time remaining (or <see cref="System.Threading.Timeout.Infinite"/> if there is no timeout for this instance).
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

        /// <inheritdoc />
        public override string ToString()
        {
            if (mStopwatch == null) return $"{nameof(cMethodControl)}({CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled})";
            return $"{nameof(cMethodControl)}({mStopwatch.ElapsedMilliseconds}/{mTimeout},{CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled})";
        }
    }
}