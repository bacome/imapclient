using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters to control batch sizes in long running operations.
    /// </summary>
    /// <seealso cref="cIMAPClient.NetworkWriteConfiguration"/>
    /// <seealso cref="cIMAPClient.AppendStreamReadConfiguration"/>
    /// <seealso cref="cIMAPClient.FetchCacheItemsConfiguration"/>
    /// <seealso cref="cIMAPClient.FetchBodyReadConfiguration"/>
    /// <seealso cref="cIMAPClient.FetchBodyWriteConfiguration"/>
    /// <seealso cref="cBodyFetchConfiguration.Write"/>
    public class cBatchSizerConfiguration
    {
        /**<summary>The minimum batch size.</summary>*/
        public readonly int Min;
        /**<summary>The maximum batch size.</summary>*/
        public readonly int Max;
        /**<summary>The maximum time that a batch should take, in milliseconds.</summary>*/
        public readonly int MaxTime;
        /**<summary>The initial batch size.</summary>*/
        public readonly int Initial;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMin">The minimum batch size.</param>
        /// <param name="pMax">The maximum batch size.</param>
        /// <param name="pMaxTime">The maximum time that a batch should take, in milliseconds.</param>
        /// <param name="pInitial">The initial batch size.</param>
        public cBatchSizerConfiguration(int pMin, int pMax, int pMaxTime, int pInitial)
        {
            if (pMin < 1) throw new ArgumentOutOfRangeException(nameof(pMin));
            if (pMax < pMin) throw new ArgumentOutOfRangeException(nameof(pMax));
            if (pMaxTime < 1) throw new ArgumentOutOfRangeException(nameof(pMaxTime));
            if (pInitial < 1) throw new ArgumentOutOfRangeException(nameof(pInitial));

            Min = pMin;
            Max = pMax;
            MaxTime = pMaxTime;
            Initial = pInitial;
        }

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString() => $"{nameof(cBatchSizerConfiguration)}({Min},{Max},{MaxTime},{Initial})";
    }
}