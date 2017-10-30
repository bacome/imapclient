using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cBinarySizes : ReadOnlyDictionary<string, uint>
    {
        // wrapper: for passing out

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