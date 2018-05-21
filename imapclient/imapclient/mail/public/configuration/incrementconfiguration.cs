using System;
using System.Threading;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress-increment callback.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cIncrementConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. Invoked once for each batch of work in the operation, the argument specifies the size of the batch.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        /// <summary>
        /// Initialises a new instance with the specified timeout. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        public cIncrementConfiguration(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token and progress-increment callback. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        public cIncrementConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }

        public cIncrementConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }

        public override string ToString() => $"{nameof(cIncrementConfiguration)}({Timeout},{CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled},{Increment != null})";

        public static implicit operator cIncrementConfiguration(int pTimeout) => new cIncrementConfiguration(pTimeout);
        public static implicit operator cIncrementConfiguration(CancellationToken pCancellationToken) => new cIncrementConfiguration(pCancellationToken);
    }
}