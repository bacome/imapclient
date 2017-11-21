using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// A read-only mapping from a message body-part that can be fetched using the BINARY (RFC 3516) command to the decoded size in bytes of that body-part.
    /// </summary>
    /// <seealso cref="cAttachment.SaveSizeInBytes"/>
    /// <seealso cref="cMessage.FetchSizeInBytes(cSinglePartBody)"/>
    /// <seealso cref="iMessageHandle.BinarySizes"/>
    public class cBinarySizes : ReadOnlyDictionary<string, uint>
    {
        internal static readonly cBinarySizes None = new cBinarySizes(new Dictionary<string, uint>());

        internal cBinarySizes(IDictionary<string, uint> pDictionary) : base(pDictionary) { }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBinarySizes));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Combines two maps.
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