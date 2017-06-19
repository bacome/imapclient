using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{ 
    public class cBinarySizesBuilder
    {
        private readonly Dictionary<string, uint> mDictionary;

        public cBinarySizesBuilder(IDictionary<string, uint> pDictionary = null)
        {
            if (pDictionary == null) mDictionary = new Dictionary<string, uint>(); // case insensitivity is not required because the key is a part (which is a dotted number)
            else mDictionary = new Dictionary<string, uint>(pDictionary);
        }

        public void Set(string pPart, uint pSize)
        {
            if (mDictionary.ContainsKey(pPart)) return;
            mDictionary.Add(pPart, pSize);
        }

        public cBinarySizes AsBinarySizes() => new cBinarySizes(mDictionary);
    }
}