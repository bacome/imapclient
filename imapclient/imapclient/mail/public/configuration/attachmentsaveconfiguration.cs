using System;
using System.Threading;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-setmaximum and progress-increment callbacks and write-size configuration.
    /// </summary>
    public class cAttachmentSaveConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-setmaximum callback for the operation. May be <see langword="null"/>. Invoked once before any progress-increment invokes, the argument specifies how many bytes there will be <see cref="Increment"/> calls for.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<long> SetMaximum;

        public readonly Action<int> Increment;

        /// <summary>
        /// The write-size configuration. May be <see langword="null"/>.
        /// </summary>
        public readonly cBatchSizerConfiguration WriteConfiguration;

        /// <summary>
        /// Initialises a new instance with the specified timeout and optional write-size configuration. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        /// <param name="pWrite"></param>
        public cAttachmentSaveConfiguration(int pTimeout, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            SetMaximum = null;
            Increment = null;
            WriteConfiguration = pWriteConfiguration;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-setmaximum and progress-increment callbacks and optional write-size configuration. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum">May be <see langword="null"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        /// <param name="pWriteConfiguration"></param>
        public cAttachmentSaveConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            SetMaximum = pSetMaximum;
            Increment = pIncrement;
            WriteConfiguration = pWriteConfiguration;
        }

        public cAttachmentSaveConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement, cBatchSizerConfiguration pWriteConfiguration)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            SetMaximum = pSetMaximum;
            Increment = pIncrement;
            WriteConfiguration = pWriteConfiguration;
        }
    }
}