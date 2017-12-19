using System;
using System.Threading;
using work.bacome.imapclient.support;
using work.bacome.imapclient.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress-increment callback. Intended for use when doing large message cache population operations.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    /// <seealso cref="cIMAPClient.Fetch(System.Collections.Generic.IEnumerable{cMessage}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{support.iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    public class cCacheItemFetchConfiguration
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
        public cCacheItemFetchConfiguration(int pTimeout)
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
        public cCacheItemFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }
    }

    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-increment callback and write-size configuration. Intended for use when fetching large message body parts into a stream.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    /// <seealso cref="cMailbox.UIDFetch(cUID, cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMessage.Fetch(cSinglePartBody, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMessage.Fetch(cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cAttachment.SaveAs(string, cBodyFetchConfiguration)"/>
    public class cBodyFetchConfiguration
    {
        /// <inheritdoc cref="cCacheItemFetchConfiguration.Timeout"/>
        public readonly int Timeout;

        /// <inheritdoc cref="cCacheItemFetchConfiguration.CancellationToken"/>
        public readonly CancellationToken CancellationToken;

        /// <inheritdoc cref="cCacheItemFetchConfiguration.Increment"/>
        public readonly Action<int> Increment;

        /// <summary>
        /// The output-stream-write batch-size configuration. If <see langword="null"/> then <see cref="cIMAPClient.FetchBodyWriteConfiguration"/> will be used.
        /// </summary>
        public readonly cBatchSizerConfiguration Write;

        /// <summary>
        /// Initialises a new instance with the specified timeout and optional output-stream-write batch-size configuration. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        /// <param name="pWrite">If <see langword="null"/> then <see cref="cIMAPClient.FetchBodyWriteConfiguration"/> will be used.</param>
        public cBodyFetchConfiguration(int pTimeout, cBatchSizerConfiguration pWrite = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            Write = pWrite;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-increment callback and optional output-stream-write batch-size configuration. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        /// <param name="pWrite">If <see langword="null"/> then <see cref="cIMAPClient.FetchBodyWriteConfiguration"/> will be used.</param>
        public cBodyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cBatchSizerConfiguration pWrite = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            Write = pWrite;
        }
    }

    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-setmaximum and progress-increment callbacks. Intended for use when retrieving a large number of messages from the server.
    /// </summary>
    /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
    public class cMessageFetchConfiguration : cCacheItemFetchConfiguration
    {
        /// <summary>
        /// The progress-setmaximum callback for the operation. May be <see langword="null"/>. Invoked once before any progress-increment invokes, the argument specifies how many messages are going to be fetched.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> SetMaximum;

        /// <inheritdoc cref="cCacheItemFetchConfiguration(int)"/>
        public cMessageFetchConfiguration(int pTimeout) : base(pTimeout)
        {
            SetMaximum = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified cancellation token, progress-setmaximum and progress-increment callbacks. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetMaximum">May be <see langword="null"/>.</param>
        /// <param name="pIncrement">May be <see langword="null"/>.</param>
        public cMessageFetchConfiguration(CancellationToken pCancellationToken, Action<int> pSetMaximum, Action<int> pIncrement) : base(pCancellationToken, pIncrement)
        {
            SetMaximum = pSetMaximum;
        }
    }
}