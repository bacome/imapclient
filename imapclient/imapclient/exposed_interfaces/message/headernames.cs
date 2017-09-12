using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderNames : IReadOnlyList<string>
    {
        public const string Importance = "importance";

        public static readonly cHeaderNames None = new cHeaderNames();

        private readonly ReadOnlyCollection<string> mNames; // not null, no duplicates, no nulls, all uppercase, sorted, may be empty

        private cHeaderNames()
        {
            mNames = new ReadOnlyCollection<string>(new List<string>());
        }

        public cHeaderNames(params string[] pNames)
        {
            mNames = ZCtor(pNames);
        }

        public cHeaderNames(IEnumerable<string> pNames)
        {
            mNames = ZCtor(pNames);
        }

        public bool Contains(string pName) => mNames.Contains(pName.ToUpperInvariant());

        public cHeaderNames Union(cHeaderNames pOther) => new cHeaderNames(mNames.Union(pOther.mNames));
        public cHeaderNames Intersect(cHeaderNames pOther) => new cHeaderNames(mNames.Intersect(pOther.mNames));
        public cHeaderNames Except(cHeaderNames pOther) => new cHeaderNames(mNames.Except(pOther.mNames));

        private ReadOnlyCollection<string> ZCtor(IEnumerable<string> pNames)
        {
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));

            List<string> lUpperValidNames = new List<string>();

            foreach (var lName in pNames)
            {
                if (string.IsNullOrEmpty(lName)) throw new ArgumentOutOfRangeException(nameof(pNames));
                foreach (char lChar in lName) if (lChar <= ' ' || lChar == ':' || lChar > '~') throw new ArgumentOutOfRangeException(nameof(pNames));
                lUpperValidNames.Add(lName.ToUpperInvariant());
            }

            lUpperValidNames.Sort();

            List<string> lDistinctSortedNames = new List<string>();

            string lLastName = null;

            foreach (var lName in lUpperValidNames)
            {
                if (lName != lLastName)
                {
                    lDistinctSortedNames.Add(lName);
                    lLastName = lName;
                }
            }

            return lDistinctSortedNames.AsReadOnly();
        }

        public string this[int pIndex] => mNames[pIndex];
        public int Count => mNames.Count;
        public IEnumerator<string> GetEnumerator() => mNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object pObject) => this == pObject as cHeaderNames;

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
            var lBuilder = new cListBuilder(nameof(cHeaderNames));
            foreach (var lName in mNames) lBuilder.Append(lName);
            return lBuilder.ToString();
        }

        public static bool operator ==(cHeaderNames pA, cHeaderNames pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            if (pA.Count != pB.Count) return false;
            for (int i = 0; i < pA.Count; i++) if (pA[i] != pB[i]) return false;
            return true;
        }

        public static bool operator !=(cHeaderNames pA, cHeaderNames pB) => !(pA == pB);
    }
}