using System;
using System.Threading;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>Allows specification of operation specific controls and callbacks.</para>
    /// </summary>
    public class cPropertyFetchConfiguration
    {
        /**<summary>The timeout for the operation.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// <para>The progress increment callback for the operation</para>
        /// <para>Called many times with an integer specifying the number of messages fetched since the last call.</para>
        /// <para>If <see cref="cIMAPClient.SynchronizationContext"/> is set, the callback will be made on that synchronisation context.</para>
        /// </summary>
        public readonly Action<int> Increment;

        public cPropertyFetchConfiguration(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
        }

        public cPropertyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
        }
    }

    /// <summary>
    /// <para>Allows specification of operation specific controls and callbacks.</para>
    /// </summary>
    public class cBodyFetchConfiguration
    {
        /**<summary>The timeout for the operation.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// <para>The progress increment callback for the operation</para>
        /// <para>Called many times with an integer specifying the number of messages fetched since the last call.</para>
        /// <para>If <see cref="cIMAPClient.SynchronizationContext"/> is set, the callback will be made on that synchronisation context.</para>
        /// </summary>
        public readonly Action<int> Increment;

        /**<summary>The configuration for controlling the output stream batch/ buffer size.</summary>*/
        public readonly cBatchSizerConfiguration Write;

        public cBodyFetchConfiguration(int pTimeout, cBatchSizerConfiguration pWrite = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            Write = pWrite;
        }

        public cBodyFetchConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cBatchSizerConfiguration pWrite = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            Write = pWrite;
        }
    }

    /// <summary>
    /// <para>Allows specification of operation specific controls and callbacks.</para>
    /// </summary>
    public class cMessageFetchConfiguration : cPropertyFetchConfiguration
    {
        /// <summary>
        /// <para>The progress initialisation callback for the operation</para>
        /// <para>Called once at the begining of the operation with an integer specifying the number of messages that will be fetched.</para>
        /// <para>If <see cref="cIMAPClient.SynchronizationContext"/> is set, the callback will be made on that synchronisation context.</para>
        /// </summary>
        public readonly Action<int> SetCount;

        public cMessageFetchConfiguration(int pTimeout) : base(pTimeout)
        {
            SetCount = null;
        }

        public cMessageFetchConfiguration(CancellationToken pCancellationToken, Action<int> pSetCount, Action<int> pIncrement) : base(pCancellationToken, pIncrement)
        {
            SetCount = pSetCount;
        }
    }
}