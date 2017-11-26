using System;
using work.bacome.imapclient.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters to control batch sizes in long running operations.
    /// </summary>
    /// <seealso cref="cIMAPClient.NetworkWriteConfiguration"/>
    /// <seealso cref="cIMAPClient.FetchCacheItemsConfiguration"/>
    /// <seealso cref="cIMAPClient.FetchBodyReadConfiguration"/>
    /// <seealso cref="cIMAPClient.FetchBodyWriteConfiguration"/>
    /// <seealso cref="cIMAPClient.AppendStreamReadConfiguration"/>
    /// <seealso cref="cBodyFetchConfiguration"/>
    public class cBatchSizerConfiguration : IEquatable<cBatchSizerConfiguration>
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cBatchSizerConfiguration pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cBatchSizerConfiguration;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Min.GetHashCode();
                lHash = lHash * 23 + Max.GetHashCode();
                lHash = lHash * 23 + MaxTime.GetHashCode();
                lHash = lHash * 23 + Initial.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cBatchSizerConfiguration)}({Min},{Max},{MaxTime},{Initial})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cBatchSizerConfiguration pA, cBatchSizerConfiguration pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Min == pB.Min && pA.Max == pB.Max && pA.MaxTime == pB.MaxTime && pA.Initial == pB.Initial;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cBatchSizerConfiguration pA, cBatchSizerConfiguration pB) => !(pA == pB);
    }
}