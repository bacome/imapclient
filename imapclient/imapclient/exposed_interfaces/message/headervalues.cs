﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderValues
    {
        private readonly cHeaderNames mNames;
        private readonly bool mNot;
        private readonly ReadOnlyDictionary<string, cStrings> mDictionary;

        public cHeaderValues(cHeaderNames pNames, bool pNot, Dictionary<string, List<string>> pDictionary)
        {
            mNames = pNames ?? throw new ArgumentNullException(nameof(pNames));
            mNot = pNot;

            if (pDictionary == null) throw new ArgumentNullException(nameof(pDictionary));

            Dictionary<string, cStrings> lDictionary = new Dictionary<string, cStrings>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var lEntry in pDictionary) lDictionary.Add(lEntry.Key, new cStrings(lEntry.Value));
            mDictionary = new ReadOnlyDictionary<string, cStrings>(lDictionary);

            // NOTE: this is not intended to be passed in: if it were then the lists of strings would need to be cloned before wrapping
        }

        private cHeaderValues(cHeaderNames pNames, bool pNot, Dictionary<string, cStrings> pDictionary)
        {
            mNames = pNames ?? throw new ArgumentNullException(nameof(pNames));
            mNot = pNot;
            if (pDictionary == null) throw new ArgumentNullException(nameof(pDictionary));
            mDictionary = new ReadOnlyDictionary<string, cStrings>(pDictionary);
        }

        public bool Contains(string pName) => mNot != mNames.Contains(pName);

        public bool ContainsAll(cHeaderNames pNames)
        {
            if (mNot) return pNames.Intersect(mNames).Count == 0;
            else return pNames.Except(mNames).Count == 0;
        }

        public cHeaderNames Missing(cHeaderNames pNames)
        {
            if (mNot) return pNames.Intersect(mNames);
            else return pNames.Except(mNames);
        }

        public bool TryGetValue(string pName, out cStrings rValue)
        {
            if (!Contains(pName)) { rValue = null; return false; }
            if (mDictionary.TryGetValue(pName, out rValue)) return true;
            rValue = null;
            return true;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHeaderValues));
            lBuilder.Append(mNames);
            lBuilder.Append(mNot);
            foreach (var lFieldValue in mDictionary) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }

        public static cHeaderValues operator +(cHeaderValues pA, cHeaderValues pB)
        {
            if (pA == null) return pB;
            if (pB == null) return pA;

            if (pA.mNames == null) return pA; // pA has all headers
            if (pB.mNames == null) return pB; // pB has all headers

            if (pA.mNames == pB.mNames && pA.mNot == pB.mNot) return pA; // they are the same, return either one

            // join the dictionaries
            Dictionary<string, cStrings> lDictionary = new Dictionary<string, cStrings>(pA.mDictionary);
            foreach (var lEntry in pB.mDictionary) if (!lDictionary.ContainsKey(lEntry.Key)) lDictionary.Add(lEntry.Key, lEntry.Value);

            // work out which fields we have values for

            if (pA.mNot)
            {
                if (pB.mNot)
                {
                    // pA contains all headers except some, pB contains all headers except some
                    return new cHeaderValues(pA.mNames.Intersect(pB.mNames), true, lDictionary);
                }
                else
                {
                    // pA contains all headers except some, pB contains a named list 
                    return new cHeaderValues(pA.mNames.Except(pB.mNames), true, lDictionary);
                }
            }
            else
            {
                if (pB.mNot)
                {
                    // pA contains a named list, pB contains all headers except some
                    return new cHeaderValues(pB.mNames.Except(pA.mNames), true, lDictionary);
                }
                else
                {
                    // pA contains a subset of header values and pB does too, add them together
                    return new cHeaderValues(pA.mNames.Union(pB.mNames), false, lDictionary);
                }
            }
        }









        public static void _Tests()
        {
            ;?;

            // especially contains, containsall and the various join types
        }
    }
}