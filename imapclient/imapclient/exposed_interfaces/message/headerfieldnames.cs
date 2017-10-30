using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public static class kHeaderFieldName
    {
        public const string InReplyTo = "In-Reply-To";
        public const string MessageId = "Message-Id";
        public const string References = "References";
        public const string Importance = "Importance";
    }

    public class cHeaderFieldNames : IReadOnlyCollection<string>
    {
        // immutable (for passing in and out)

        public static readonly cHeaderFieldNames None = new cHeaderFieldNames();
        public static readonly cHeaderFieldNames References = new cHeaderFieldNames(kHeaderFieldName.References);
        public static readonly cHeaderFieldNames Importance = new cHeaderFieldNames(kHeaderFieldName.Importance);

        private readonly cHeaderFieldNameList mNames;

        private cHeaderFieldNames() => mNames = new cHeaderFieldNameList();
        public cHeaderFieldNames(params string[] pNames) => mNames = new cHeaderFieldNameList(pNames); // validates, duplicates, removes duplicates
        public cHeaderFieldNames(IEnumerable<string> pNames) => mNames = new cHeaderFieldNameList(pNames); // validates, duplicates, removes duplicates
        public cHeaderFieldNames(cHeaderFieldNameList pNames) => mNames = new cHeaderFieldNameList(pNames); // duplicates
        private cHeaderFieldNames(cHeaderFieldNameList pNames, bool pWrap) => mNames = pNames; // wraps

        public bool Contains(string pName) => mNames.Contains(pName);
        public bool Contains(params string[] pNames) => mNames.Contains(pNames);
        public bool Contains(IEnumerable<string> pNames) => mNames.Contains(pNames);

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

        public static implicit operator cHeaderFieldNames(cHeaderFieldNameList pNames) => new cHeaderFieldNames(pNames);

        public static bool TryConstruct(IEnumerable<string> pNames, out cHeaderFieldNames rNames)
        {
            if (!cHeaderFieldNameList.TryConstruct(pNames, out var lNames)) { rNames = null; return false; }
            rNames = new cHeaderFieldNames(lNames, true);
            return true;
        }
    }

    public class cHeaderFieldNameList : IReadOnlyCollection<string>
    {
        // implements case insensitivity
        //  implements only one copy of each header field
        //  implements the grammar for header field names

        private readonly List<string> mNames;

        public cHeaderFieldNameList()
        {
            mNames = new List<string>();
        }

        public cHeaderFieldNameList(params string[] pNames) // validates, duplicates, removes duplicates
        {
            if (pNames == null)
            {
                mNames = new List<string>();
                return;
            }

            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            mNames = new List<string>(pNames.Distinct(StringComparer.InvariantCultureIgnoreCase));
        }

        public cHeaderFieldNameList(IEnumerable<string> pNames) // validates, duplicates, removes duplicates
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            mNames = new List<string>(pNames.Distinct(StringComparer.InvariantCultureIgnoreCase));
        }

        public cHeaderFieldNameList(cHeaderFieldNameList pNames) // duplicates
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            mNames = new List<string>(pNames.mNames);
        }

        private cHeaderFieldNameList(IEnumerable<string> pNames, bool pUnique) // duplicates, optionally removes duplicates
        {
            if (pUnique) mNames = new List<string>(pNames);
            else mNames = new List<string>(pNames.Distinct(StringComparer.InvariantCultureIgnoreCase));
        }

        public bool Contains(string pName) => mNames.Contains(pName, StringComparer.InvariantCultureIgnoreCase);

        public bool Contains(params string[] pNames) => ZContains(pNames);
        public bool Contains(IEnumerable<string> pNames) => ZContains(pNames);

        private bool ZContains(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) if (!Contains(lName)) return false;
            return true;
        }

        public void Add(string pName)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (!ZIsValidName(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!Contains(pName)) mNames.Add(pName);
        }

        public void Add(params string[] pNames) => ZAdd(pNames);
        public void Add(IEnumerable<string> pNames) => ZAdd(pNames);

        private void ZAdd(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            foreach (var lName in pNames) if (!ZIsValidName(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
            foreach (var lName in pNames) if (!Contains(lName)) mNames.Add(lName);
        }

        public void Remove(string pName) => mNames.RemoveAll(n => n.Equals(pName, StringComparison.InvariantCultureIgnoreCase));

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

        private static bool ZIsValidName(string pName)
        {
            if (pName == null) return false;
            if (pName.Length == 0) return false;
            foreach (char lChar in pName) if (!cCharset.FText.Contains(lChar)) return false;
            return true;
        }

        public static bool TryConstruct(IEnumerable<string> pNames, out cHeaderFieldNameList rNames)
        {
            if (pNames == null) { rNames = null; return false; }
            foreach (var lName in pNames) if (!ZIsValidName(lName)) { rNames = null; return false; }
            rNames = new cHeaderFieldNameList(pNames, false);
            return true;
        }









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