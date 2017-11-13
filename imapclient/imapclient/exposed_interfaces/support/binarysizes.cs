using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// A mapping from a message part to a size in bytes for message parts that can be fetched using the BINARY (RFC 3516) command.
    /// </summary>
    /// <remarks>
    /// Using the <see cref="cMessage.FetchSizeInBytes(cSinglePartBody)"/> or <see cref="cAttachment.SaveSizeInBytes"/> methods may create values in this map.
    /// </remarks>
    /// <seealso cref="iMessageHandle.BinarySizes"/>
    public class cBinarySizes : ReadOnlyDictionary<string, uint>
    {
        // wrapper: for passing out

        internal static readonly cBinarySizes None = new cBinarySizes(new Dictionary<string, uint>());

        internal cBinarySizes(IDictionary<string, uint> pDictionary) : base(pDictionary) { }

        /**<summary>Returns a string that represents the map.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBinarySizes));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Combine two maps.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static cBinarySizes operator +(cBinarySizes pA, cBinarySizes pB)
        {
            if (pA == null || pA.Count == 0) return pB ?? None; // pA is null or None
            if (pB == null || pB.Count == 0) return pA; // pB is null or None

            Dictionary<string, uint> lDictionary = new Dictionary<string, uint>(pA);
            foreach (var lEntry in pB) lDictionary[lEntry.Key] = lEntry.Value;
            return new cBinarySizes(lDictionary);
        }
    }
}