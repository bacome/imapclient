using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>A mapping from a message part to a size in bytes for message parts that can be fetched using the IMAP BINARY command (RFC 3516).</para>
    /// <para>Using the <see cref="cMessage.FetchSizeInBytes(cSinglePartBody)"/> or <see cref="cAttachment.SaveSizeInBytes"/> methods may create values in this map.</para>
    /// </summary>
    public class cBinarySizes : ReadOnlyDictionary<string, uint>
    {
        // wrapper: for passing out

        /// <summary>
        /// An empty mapping.
        /// </summary>
        public static readonly cBinarySizes None = new cBinarySizes(new Dictionary<string, uint>());

        public cBinarySizes(IDictionary<string, uint> pDictionary) : base(pDictionary) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBinarySizes));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }

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