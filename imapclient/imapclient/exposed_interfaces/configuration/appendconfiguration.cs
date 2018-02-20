using System;
using System.Threading;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-setmaximum and progress-increment callbacks.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    /// <seealso cref="cMailbox.Append(cAppendData, cAppendConfiguration)"/>
    /// <seealso cref="cMailbox.Append(System.Collections.Generic.IEnumerable{cAppendData}, cAppendConfiguration)"/>
    public class cAppendConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-setmaximum callback for the operation. May be <see langword="null"/>. 
        /// Invoked once after all quoted-printable conversions and before any sending to the server, the argument specifies how many bytes are going to be sent to the server.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<long> SetMaximum;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. 
        /// Invoked once for each batch of bytes converted to quoted-printable format on the client-side (before sending to the server starts), the argument specifies how many input bytes were converted.
        /// Invoked once for each batch of bytes sent to the server (after the <see cref="SetMaximum"/> invoke), the argument specifies how many bytes were sent in the batch.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        /// <summary>
        /// Initialises a new instance with the specified timeout. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        public cAppendConfiguration(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            SetMaximum = null;
            Increment = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-setmaximum and progress-increment callbacks. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum">May be <see langword="null"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        public cAppendConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            SetMaximum = pSetMaximum;
            Increment = pIncrement;
        }
    }
}