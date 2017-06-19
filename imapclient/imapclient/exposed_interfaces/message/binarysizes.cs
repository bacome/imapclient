using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cBinarySizes : ReadOnlyDictionary<string, uint>
    {
        public cBinarySizes(IDictionary<string, uint> pDictionary) : base(pDictionary) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBinarySizes));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }

        public static cBinarySizes operator +(cBinarySizes pA, cBinarySizes pB)
        {
            if (pA == null) return pB;
            if (pB == null) return pA;
            cBinarySizesBuilder lBuilder = new cBinarySizesBuilder(pA);
            foreach (var lEntry in pB) lBuilder.Set(lEntry.Key, lEntry.Value);
            return lBuilder.AsBinarySizes();
        }
    }
}