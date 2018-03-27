using System;
using System.Threading;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-increment callback and write-size configuration. Intended for use when fetching large message body parts into a stream.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cFetchConfiguration
    {
        public readonly int Timeout;

        public readonly CancellationToken CancellationToken;

        public readonly Action<int> Increment;

        /// <summary>
        /// The write-size configuration. May be <see langword="null"/>.
        /// </summary>
        public readonly cBatchSizerConfiguration WriteConfiguration;

        /// <summary>
        /// Initialises a new instance with the specified timeout and optional write-size configuration. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        /// <param name="pWrite">If <see langword="null"/> then <see cref="cMailClient.LocalStreamWriteConfiguration"/> will be used.</param>
        public cFetchConfiguration(int pTimeout, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            WriteConfiguration = pWriteConfiguration;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-increment callback and optional write-size configuration. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        /// <param name="pWrite">If <see langword="null"/> then <see cref="cMailClient.LocalStreamWriteConfiguration"/> will be used.</param>
        public cFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            WriteConfiguration = pWriteConfiguration;
        }

        public cFetchConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement, cBatchSizerConfiguration pWriteConfiguration)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            WriteConfiguration = pWriteConfiguration;
        }
    }

}