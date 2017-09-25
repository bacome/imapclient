using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cHeaderFieldNames : IReadOnlyCollection<string>
    {
        // immutable (for passing in and out)

        public const string InReplyTo = "In-RePlY-tO";
        public const string MessageId = "MeSsAgE-Id";
        public const string References = "ReFeReNcEs";
        public const string Importance = "ImPoRtAnCe";

        public static readonly cHeaderFieldNames None = new cHeaderFieldNames();

        private readonly cHeaderFieldNameList mNames;

        public cHeaderFieldNames(params string[] pNames) => mNames = new cHeaderFieldNameList(pNames);
        public cHeaderFieldNames(IEnumerable<string> pNames) => mNames = new cHeaderFieldNameList(pNames);
        public cHeaderFieldNames(cHeaderFieldNameList pNames) => mNames = new cHeaderFieldNameList(pNames);
        private cHeaderFieldNames(cHeaderFieldNameList pNames, bool pNew) => mNames = pNames;

        public bool Contains(string pName) => mNames.Contains(pName);
        public bool Contains(params string[] pFlags) => mNames.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mNames.Contains(pFlags);

        public cHeaderFieldNames Union(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Union(pOther.mNames), true);
        public cHeaderFieldNames Intersect(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Intersect(pOther.mNames), true);
        public cHeaderFieldNames Except(cHeaderFieldNames pOther) => new cHeaderFieldNames(mNames.Except(pOther.mNames), true);

        public int Count => mNames.Count;
        public IEnumerator<string> GetEnumerator() => mNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => mNames.ToString();

        public override bool Equals(object pObject) => this == pObject as cHeaderFieldNames;

        public override int GetHashCode() => mNames.GetHashCode();

        public static bool operator ==(cHeaderFieldNames pA, cHeaderFieldNames pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.mNames == pB.mNames;
        }

        public static bool operator !=(cHeaderFieldNames pA, cHeaderFieldNames pB) => !(pA == pB);





















        /*
        public static cHeaderFieldNames operator |(cHeaderFieldNames pNames, string pName)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            if (pName == null) throw new ArgumentNullException(nameof(pName));

            if (!ZTryNormaliseName(pName, out var lName)) throw new ArgumentOutOfRangeException(nameof(pName));

            ;?; // case 
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

            lNames.Sort(); ;?; // case insen

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



        public static implicit operator cSettableFlags(cSettableFlagList pFlags) => new cSettableFlags(pFlags);






        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cHeaderFieldNames), nameof(_Tests));

            cHeaderFieldNameList lNames1;
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
    */

    }

    public class cHeaderFieldNameList : IReadOnlyCollection<string>
    {
        // implements case insensitivity
        //  implements only one copy of each header field
        //  implements the grammar for header field names

        private readonly List<string> mNames;

        public cHeaderFieldNameList(IEnumerable<string> pNames = null)
        {
            mNames = new List<string>();
            if (pNames == null) return;
            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            foreach (var lName in pNames) if (!Contains(lName)) mNames.Add(lName);
        }

        public cHeaderFieldNameList(cHeaderFieldNameList pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            mNames = new List<string>(pNames.mNames);
        }

        private cHeaderFieldNameList(IEnumerable<string> pNames, bool pUnique)
        {
            if (pUnique)
            {
                mNames = new List<string>(pNames);
                return;
            }

            mNames = new List<string>();
            foreach (var lName in pNames) if (!Contains(lName)) mNames.Add(lName);
        }

        public bool Contains(string pName)
        {
            if (pName == null || pName.Length == 0) return false;
            return mNames.Contains(pName, StringComparer.InvariantCultureIgnoreCase);
        }

        public bool Contains(params string[] pNames) => ZContains(pNames);
        public bool Contains(IEnumerable<string> pNames) => ZContains(pNames);

        private bool ZContains(IEnumerable<string> pNames)
        {
            if (pNames == null) return false;
            foreach (var lName in pNames) if (!Contains(lName)) return false;
            return true;
        }

        public void Add(string pName)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (Contains(pName)) return;
            if (!ZIsValidName(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mNames.Add(pName);
        }

        public void Add(params string[] pNames) => ZAdd(pNames);
        public void Add(IEnumerable<string> pNames) => ZAdd(pNames);

        private void ZAdd(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            foreach (var lName in pNames) if (!Contains(lName)) mNames.Add(lName);
        }

        public void Remove(string pName)
        {
            if (pName == null || pName.Length == 0) return;
            mNames.RemoveAll(n => n.Equals(pName, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Remove(params string[] pNames) => ZRemove(pNames);
        public void Remove(IEnumerable<string> pNames) => ZRemove(pNames);

        private void ZRemove(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) Remove(lName);
        }

        public cHeaderFieldNameList Union(cHeaderFieldNameList pOther) => new cHeaderFieldNameList(mNames.Union(pOther.mNames, StringComparer.InvariantCultureIgnoreCase), true);
        public cHeaderFieldNameList Intersect(cHeaderFieldNameList pOther) => new cHeaderFieldNameList(mNames.Intersect(pOther.mNames, StringComparer.InvariantCultureIgnoreCase), true);
        public cHeaderFieldNameList Except(cHeaderFieldNameList pOther) => new cHeaderFieldNameList(mNames.Except(pOther.mNames, StringComparer.InvariantCultureIgnoreCase), true);

        public int Count => mNames.Count;
        public IEnumerator<string> GetEnumerator() => mNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mNames.GetEnumerator();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHeaderFieldNameList));
            foreach (var lName in mNames) lBuilder.Append(lName);
            return lBuilder.ToString();
        }

        public static bool TryConstruct(IEnumerable<string> pNames, out cHeaderFieldNameList rNames)
        {
            if (pNames == null) { rNames = null; return false; }
            foreach (var lName in pNames) if (!ZIsValidName(lName)) { rNames = null; return false; }
            rNames = new cHeaderFieldNameList(pNames, false);
            return true;
        }

        private static bool ZIsValidName(string pName)
        {
            if (pName == null) return false;
            if (pName.Length == 0) return false;
            foreach (char lChar in pName) if (!cCharset.FText.Contains(lChar)) return false;
            return true;
        }

        public override bool Equals(object pObject) => this == pObject as cHeaderFieldNameList;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (var lName in mNames) lHash = lHash * 23 + lName.ToUpperInvariant().GetHashCode();
                return lHash;
            }
        }

        public static bool operator ==(cHeaderFieldNameList pA, cHeaderFieldNameList pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            if (pA.mNames.Count != pB.mNames.Count) return false;
            foreach (var lName in pA.mNames) if (!pB.mNames.Contains(lName, StringComparer.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        public static bool operator !=(cHeaderFieldNameList pA, cHeaderFieldNameList pB) => !(pA == pB);











        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cHeaderFieldNameList), nameof(_Tests));

            cHeaderFieldNameList lNames1;
            cHeaderFieldNameList lNames2;
            cHeaderFieldNameList lNames3;
            cHeaderFieldNameList lNames4;

            if (!TryConstruct(new string[] { }, out lNames1) || lNames1.Count != 0) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.1");
            if (!TryConstruct(new string[] { "fred", "angus" }, out lNames1) || !TryConstruct(new string[] { "AnGuS", "ANGUS", "FrEd" }, out lNames2) || !lNames1.Contains(lNames2) || !lNames2.Contains(lNames1)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.2");
            if (!TryConstruct(new string[] { "fred", "charlie" }, out lNames3) || !TryConstruct(new string[] { "CHARLie", "mAx" }, out lNames4) || lNames3.Contains(lNames4) || lNames4.Contains(lNames3)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.3");
            if (lNames2.Contains("max") || !lNames2.Contains("FREd") || !lNames4.Contains("max") || lNames4.Contains("FREd")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.4");

            lNames2 = new cHeaderFieldNameList(lNames1);
            lNames2.Add("fReD");
            if (lNames1 != lNames2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.5");

            lNames2.Add("charlie");
            if (lNames2.Count != 3 || !lNames2.Contains("Fred") || !lNames2.Contains("ANgUS") || !lNames2.Contains("CHArLIE")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.6");

            var lNames5 = lNames1.Union(lNames3);
            if (lNames5 != lNames2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.7");

            lNames2 = lNames1.Intersect(lNames3);
            if (lNames2.Count != 1 || !lNames2.Contains("fReD")) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.8");

            lNames2 = lNames5.Except(lNames4);
            if (lNames2.Count != 2 || lNames2 != lNames1) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.9");

            /*
            cHeaderFieldNames lABC = new cHeaderFieldNames("a", "b", "c");
            cHeaderFieldNames lBC = new cHeaderFieldNames("b", "c");
            cHeaderFieldNames lCDE = new cHeaderFieldNames("c", "d", "e");
            cHeaderFieldNames lEFG = new cHeaderFieldNames("e", "f", "g");

            if (!ReferenceEquals(lABC | lBC, lABC)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.10");
            if (!ReferenceEquals(lBC | lABC, lABC)) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.11");

            var lABCDE = lABC | lCDE;
            var lABCDE2 = lCDE | lABC | lBC;
            if (lABCDE.Count != 5 || lABCDE != lABCDE2) throw new cTestsException($"{nameof(cHeaderFieldNames)}.1.12"); */

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