using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using work.bacome.mailclient;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// An immutable mapping from a message body-part that can be fetched using the BINARY (RFC 3516) command to the decoded size in bytes of that body-part.
    /// </summary>
    [Serializable]
    public class cBinarySizes : ReadOnlyDictionary<string, uint>
    {
        internal static readonly cBinarySizes Empty = new cBinarySizes(new Dictionary<string, uint>());

        internal cBinarySizes(IDictionary<string, uint> pDictionary) : base(pDictionary) { }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            foreach (var lKey in Keys)
            {
                if (lKey == null) throw new cDeserialiseException(nameof(cBinarySizes), nameof(Keys), kDeserialiseExceptionMessage.ContainsNulls);
                if (!cValidation.IsValidSectionPart(lKey)) throw new cDeserialiseException(nameof(cBinarySizes), nameof(Keys), kDeserialiseExceptionMessage.ContainsInvalidValues);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBinarySizes));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Returns a map that is the combination of the two specified two maps.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static cBinarySizes operator +(cBinarySizes pA, cBinarySizes pB)
        {
            if (pA == null || pA.Count == 0) return pB ?? Empty; // pA is null or Empty
            if (pB == null || pB.Count == 0) return pA; // pB is null or Empty

            Dictionary<string, uint> lDictionary = new Dictionary<string, uint>(pA);
            foreach (var lEntry in pB) lDictionary[lEntry.Key] = lEntry.Value;
            return new cBinarySizes(lDictionary);
        }
    }
}