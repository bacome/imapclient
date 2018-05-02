using System;
using System.Threading;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress-increment callback. Intended for use when retrieving large message sections.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cCopySectionToStreamConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. Invoked once for each batch of bytes copied, the argument specifies how many bytes were copied in the batch.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        /// <summary>
        /// Initialises a new instance with the specified timeout. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        public cCopySectionToStreamConfiguration(int pTimeout)
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
        public cCopySectionToStreamConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }

        public cCopySectionToStreamConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }
    }
}