using System;
using System.Threading;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-increment callback and frequency. Intended for use when retrieving large message sections.
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

        public readonly int MaxCallbackFrequency;

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
            MaxCallbackFrequency = 100;
        }

        public cCopySectionToStreamConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, int pMaxCallbackFrequency = 100)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            MaxCallbackFrequency = pMaxCallbackFrequency;
        }

        public cCopySectionToStreamConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement, int pMaxCallbackFrequency = 100)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            MaxCallbackFrequency = pMaxCallbackFrequency;
        }
    }
}