using System;
using System.Threading;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress-increment callback. Intended for use when doing large message cache population operations.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cFetchCacheItemConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. Invoked once for each batch of messages fetched, the argument specifies how many messages were fetched in the batch.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        /// <summary>
        /// Initialises a new instance with the specified timeout. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        public cFetchCacheItemConfiguration(int pTimeout)
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
        public cFetchCacheItemConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }

        public cFetchCacheItemConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }
    }

    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-setmaximum and progress-increment callbacks. Intended for use when retrieving a large number of messages from the server.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    public class cMessageFetchCacheItemConfiguration : cFetchCacheItemConfiguration
    {
        /// <summary>
        /// The progress-setmaximum callback for the operation. May be <see langword="null"/>. Invoked once before any progress-increment invokes, the argument specifies how many messages are going to be fetched.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> SetMaximum;

        /// <inheritdoc cref="cFetchCacheItemConfiguration(int)"/>
        public cMessageFetchCacheItemConfiguration(int pTimeout) : base(pTimeout)
        {
            SetMaximum = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-setmaximum and progress-increment callbacks. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum">May be <see langword="null"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        public cMessageFetchCacheItemConfiguration(CancellationToken pCancellationToken, Action<int> pSetMaximum, Action<int> pIncrement) : base(pCancellationToken, pIncrement)
        {
            SetMaximum = pSetMaximum;
        }

        public cMessageFetchCacheItemConfiguration(int pTimeout, CancellationToken pCancellationToken, Action<int> pSetMaximum, Action<int> pIncrement) : base(pTimeout, pCancellationToken, pIncrement)
        {
            SetMaximum = pSetMaximum;
        }
    }
}