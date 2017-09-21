using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cHeaderFieldNames : IReadOnlyList<string>
    {
        public const string InReplyTo = "In-RePlY-tO";
        public const string MessageId = "MeSsAgE-Id";
        public const string References = "ReFeReNcEs";
        public const string Importance = "ImPoRtAnCe";

        public static readonly cHeaderFieldNames None = new cHeaderFieldNames();

        ;?;
        private readonly ReadOnlyCollection<string> mNames; // not null, no duplicates, no nulls, all uppercase, sorted, may be empty

        private cHeaderFieldNames()
        {
            mNames = new ReadOnlyCollection<string>(new List<string>());
        }

        private cHeaderFieldNames(ReadOnlyCollection<string> pNames)
        {
            mNames = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

        public cHeaderFieldNames(params string[] pNames)
        {
            ;?;
            if (!ZTryNormaliseNames(pNames, out mNames)) throw new ArgumentOutOfRangeException(nameof(pNames));
        }

        public cHeaderFieldNames(IEnumerable<string> pNames)
        {
            if (!ZTryNormaliseNames(pNames, out mNames)) throw new ArgumentOutOfRangeException(nameof(pNames));
        }

        public bool Contains(string pName) => mNames.Contains(pName, StringComparer.InvariantCultureIgnoreCase);

        public cHeaderFieldNames Union(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Union(pOther.mNames, StringComparer.InvariantCultureIgnoreCase));
        public cHeaderFieldNames Intersect(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Intersect(pOther.mNames, StringComparer.InvariantCultureIgnoreCase));
        public cHeaderFieldNames Except(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Except(pOther.mNames, StringComparer.InvariantCultureIgnoreCase));

        public string this[int pIndex] => mNames[pIndex];
        public int Count => mNames.Count;
        public IEnumerator<string> GetEnumerator() => mNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object pObject) => this == pObject as cHeaderFieldNames;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (string lName in mNames) lHash = lHash * 23 + lName.GetHashCode();
                return lHash;
            }
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHeaderFieldNames));
            foreach (var lName in mNames) lBuilder.Append(lName);
            return lBuilder.ToString();
        }

        public static bool operator ==(cHeaderFieldNames pA, cHeaderFieldNames pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            if (pA.Count != pB.Count) return false;
            for (int i = 0; i < pA.Count; i++) if (pA[i] != pB[i]) return false;
            return true;
        }

        public static bool operator !=(cHeaderFieldNames pA, cHeaderFieldNames pB) => !(pA == pB);

        public static cHeaderFieldNames operator |(cHeaderFieldNames pNames, string pName)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            if (pName == null) throw new ArgumentNullException(nameof(pName));

            if (!ZTryNormaliseName(pName, out var lName)) throw new ArgumentOutOfRangeException(nameof(pName));

            if (pNames.mNames.Contains(lName)) return pNames;

            List<string> lNames = new List<string>(pNames.mNames);
            lNames.Add(lName);
            lNames.Sort(StringComparer.InvariantCultureIgnoreCase);

            return new cHeaderFieldNames(lNames.AsReadOnly());
        }

        public static cHeaderFieldNames operator |(cHeaderFieldNames pA, cHeaderFieldNames pB)
        {
            if (pA == null) throw new ArgumentNullException(nameof(pA));
            if (pB == null) throw new ArgumentNullException(nameof(pB));

            List<string> lNames = new List<string>(pB.mNames.Except(pA.mNames));
            if (lNames.Count == 0) return pA;
            if (lNames.Count == pB.mNames.Count) return pB;

            lNames.AddRange(pA.mNames);

            lNames.Sort();

            return new cHeaderFieldNames(lNames.AsReadOnly());
        }

        public static bool TryConstruct(string pName, out cHeaderFieldNames rNames)
        {
            if (ZTryNormaliseName(pName, out var lName))
            {
                List<string> lNames = new List<string>(1);
                lNames.Add(lName);
                rNames = new cHeaderFieldNames(lNames.AsReadOnly());
                return true;
            }

            rNames = null;
            return false;
        }

        public static bool TryConstruct(IEnumerable<string> pNames, out cHeaderFieldNames rNames)
        {
            if (ZTryNormaliseNames(pNames, out var lNames))
            {
                rNames = new cHeaderFieldNames(lNames);
                return true;
            }

            rNames = null;
            return false;
        }

        private static bool ZTryNormaliseNames(IEnumerable<string> pNames, out ReadOnlyCollection<string> rNormalisedNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));

            List<string> lNames = new List<string>();

            foreach (var lName in pNames)
            {
                if (ZTryNormaliseName(lName, out var lNormalisedName)) lNames.Add(lNormalisedName);
                else { rNormalisedNames = null; return false; }
            }

            lNames.Sort();

            List<string> lNormalisedNames = new List<string>();

            string lLastName = null;

            foreach (var lName in lNames)
            {
                if (lName != lLastName)
                {
                    lNormalisedNames.Add(lName);
                    lLastName = lName;
                }
            }

            rNormalisedNames = lNormalisedNames.AsReadOnly();
            return true;
        }

        private static bool ZTryNormaliseName(string pName, out string rName)
        {
            if (string.IsNullOrEmpty(pName)) { rName = null; return false; }
            foreach (char lChar in pName) if (!cCharset.FText.Contains(lChar)) { rName = null; return false; }
            rName = pName.ToUpperInvariant();?;
            return true;
        }










        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cHeaderFieldNames), nameof(_Tests));

            cHeaderFieldNames lNames1;
            cHeaderFieldNames lNames2;
            cHeaderFieldNames lNames3;
            cHeaderFieldNames lNames4;

            if (!TryConstruct(new string[] { }, out lNames1) || lNames1 != None) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.1");
            if (!TryConstruct(new string[] { "fred", "angus"  }, out lNames1) || !TryConstruct(new string[] { "AnGuS", "ANGUS", "FrEd" }, out lNames2) || lNames1 != lNames2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.2");
            if (!TryConstruct(new string[] { "fred", "charlie" }, out lNames3) || !TryConstruct(new string[] { "CHARLie", "mAx" }, out lNames4) || lNames3 == lNames4) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.3");
            if (lNames2.Contains("max") || !lNames2.Contains("FREd") || !lNames4.Contains("max") || lNames4.Contains("FREd")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.4");

            lNames2 = lNames1 | "fReD";
            if (!ReferenceEquals(lNames1, lNames2)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.5");

            lNames2 = lNames1 | "charlie";
            if (ReferenceEquals(lNames1, lNames2) || !lNames2.Contains("Fred") || !lNames2.Contains("ANgUS") || !lNames2.Contains("CHArLIE") || lNames2.Count != 3) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.6");

            var lNames5 = lNames1.Union(lNames3);
            if (lNames5 != lNames2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.7");

            lNames2 = lNames1.Intersect(lNames3);
            if (lNames2.Count != 1 || !lNames2.Contains("fReD")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.8");

            lNames2 = lNames5.Except(lNames4);
            if (lNames2.Count != 2 || lNames2 != lNames1) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.9");


            cHeaderFieldNames lABC = new cHeaderFieldNames("a", "b", "c");
            cHeaderFieldNames lBC = new cHeaderFieldNames("b", "c");
            cHeaderFieldNames lCDE = new cHeaderFieldNames("c", "d", "e");
            cHeaderFieldNames lEFG = new cHeaderFieldNames("e", "f", "g");

            if (!ReferenceEquals(lABC | lBC, lABC)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.10");
            if (!ReferenceEquals(lBC | lABC, lABC)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.11");

            var lABCDE = lABC | lCDE;
            var lABCDE2 = lCDE | lABC | lBC;
            if (lABCDE.Count != 5 || lABCDE != lABCDE2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.12");

            bool lFailed = false;
            try { cHeaderFieldNames lF = new cHeaderFieldNames("dd ff"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.13");

            lFailed = false;
            try { cHeaderFieldNames lF = new cHeaderFieldNames("dd:ff"); }
            catch { lFailed = true; }
            if (!lFailed) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.14");
        }
    }
}