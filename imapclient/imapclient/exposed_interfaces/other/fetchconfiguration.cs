using System;
using System.Threading;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains an operation specific timeout, cancellation token and progress-increment callback. Intended for use when doing large message cache population operations.
    /// </summary>
    /// <seealso cref="cIMAPClient.Fetch(System.Collections.Generic.IEnumerable{cMessage}, cCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{support.iMessageHandle}, cCacheItems, cCacheItemFetchConfiguration)"/>
    public class cCacheItemFetchConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-increment callback for the operation. May be null. Invoked possibly many times with an integer specifying the number of messages fetched since the last call.
        /// </summary>
        public readonly Action<int> Increment;

        /// <summary>
        /// Initialises a new instance with a timeout only. Intended for use with synchronous APIs.
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
        /// Initialises a new instance with a cancellation token and a progress-increment callback. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement">May be null.</param>
        /// <remarks>
        /// If <see cref="cIMAPClient.SynchronizationContext"/> is non-null, then the callback is invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in the callback the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </remarks>
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
    /// <seealso cref="cMailbox.UIDFetch(cUID, cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMessage.Fetch(cSinglePartBody, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMessage.Fetch(cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cAttachment.SaveAs(string, cBodyFetchConfiguration)"/>
    public class cBodyFetchConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-increment callback for the operation. May be null. Called many times with an integer specifying the number of bytes written into the associated stream.
        /// </summary>
        public readonly Action<int> Increment;
    
        /**<summary>The configuration for controlling the size of the writes to the associated stream. May be null.</summary>*/
        public readonly cBatchSizerConfiguration Write;

        /// <summary>
        /// Initialises a new instance with a timeout and write-size configuration. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout">May be <see cref="Timeout.Infinite"/>.</param>
        /// <param name="pWrite">If null then the default <see cref="cIMAPClient.FetchBodyWriteConfiguration"/> will be used.</param>
        public cBodyFetchConfiguration(int pTimeout, cBatchSizerConfiguration pWrite = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            Write = pWrite;
        }

        /// <summary>
        /// Initialises a new instance with a cancellation token, progress-increment callback and write-size configuration. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pIncrement">May be null.</param>
        /// <param name="pWrite">If null then the default <see cref="cIMAPClient.FetchBodyWriteConfiguration"/> will be used.</param>
        /// <remarks>
        /// If <see cref="cIMAPClient.SynchronizationContext"/> is non-null, then the callback is invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in the callback the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </remarks>
        public cBodyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cBatchSizerConfiguration pWrite = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            Write = pWrite;
        }
    }

    /// <summary>
    /// Contains an operation specific timeout, cancellation token, progress-setcount callback and progress-increment callback.
    /// </summary>
    /// <remarks>
    /// Use this when the number of messages returned from a filter may be large.
    /// The progress-setcount callback should be used to initialise a progress bar so that the progress-increment callbacks give accurate operation progress feedback.
    /// </remarks>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cCacheItems, cMessageFetchConfiguration)"/>
    public class cMessageFetchConfiguration : cCacheItemFetchConfiguration
    {
        /// <summary>
        /// The progress-setcount callback for the operation. May be null. Called once, before any progress-increment callbacks, with an integer specifying the total number of messages to be fetched.
        /// </summary>
        public readonly Action<int> SetCount;

        /// <summary>
        /// Initialises a new instance with a timeout only. Intended for use with synchronous APIs.
        /// </summary>
        /// <param name="pTimeout"></param>
        public cMessageFetchConfiguration(int pTimeout) : base(pTimeout)
        {
            SetCount = null;
        }

        /// <summary>
        /// Initialises a new instance with a cancellation token, progress-setcount callback and a progress-increment callback. Intended for use with asynchronous APIs.
        /// </summary>
        /// <param name="pCancellationToken">May be <see cref="CancellationToken.None"/>.</param>
        /// <param name="pSetCount">May be null.</param>
        /// <param name="pIncrement">May be null.</param>
        /// <remarks>
        /// If <see cref="cIMAPClient.SynchronizationContext"/> is non-null, then the callbacks are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If exceptions are raised in the callbacks the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exceptions are ignored.
        /// </remarks>
        public cMessageFetchConfiguration(CancellationToken pCancellationToken, Action<int> pSetCount, Action<int> pIncrement) : base(pCancellationToken, pIncrement)
        {
            SetCount = pSetCount;
        }
    }
}