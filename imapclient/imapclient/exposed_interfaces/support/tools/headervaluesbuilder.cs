using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public class cHeaderValuesBuilder
    {
        private readonly cHeaderNames mNames;
        private readonly bool mNot;
        private readonly Dictionary<string, List<string>> mDictionary = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        public cHeaderValuesBuilder(cHeaderNames pNames, bool pNot)
        {
            mNames = pNames;
            mNot = pNot;
        }

        public void Add(string pName, string pValue)
        {
            List<string> lValues;

            if (!mDictionary.TryGetValue(pName, out lValues))
            {
                lValues = new List<string>();
                mDictionary.Add(pName, lValues);
            }

            lValues.Add(pValue);
        }

        public cHeaderValues AsHeaderValues() => new cHeaderValues(mNames, mNot, mDictionary);
    }
}