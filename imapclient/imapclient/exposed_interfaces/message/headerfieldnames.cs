using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderFieldNames : IReadOnlyList<string>
    {
        public const string MessageId = "MESSAGE-ID";
        public const string InReplyTo = "IN-REPLY-TO";
        public const string Importance = "IMPORTANCE";
        public const string References = "REFERENCES";

        public static readonly cHeaderFieldNames None = new cHeaderFieldNames();

        private readonly ReadOnlyCollection<string> mFieldNames; // not null, no duplicates, no nulls, all uppercase, sorted, may be empty

        private cHeaderFieldNames()
        {
            mFieldNames = new ReadOnlyCollection<string>(new List<string>());
        }

        private cHeaderFieldNames(ReadOnlyCollection<string> pFieldNames)
        {
            mFieldNames = pFieldNames ?? throw new ArgumentNullException(nameof(pFieldNames));
        }

        public cHeaderFieldNames(params string[] pFieldNames)
        {
            if (!ZTryNormaliseFieldNames(pFieldNames, out mFieldNames)) throw new ArgumentOutOfRangeException(nameof(pFieldNames));
        }

        public cHeaderFieldNames(IEnumerable<string> pFieldNames)
        {
            if (!ZTryNormaliseFieldNames(pFieldNames, out mFieldNames)) throw new ArgumentOutOfRangeException(nameof(pFieldNames));
        }

        public bool Contains(string pFieldName) => mFieldNames.Contains(pFieldName.ToUpperInvariant());

        public cHeaderFieldNames Union(cHeaderFieldNames pOther) => new cHeaderFieldNames(mFieldNames.Union(pOther.mFieldNames));
        public cHeaderFieldNames Intersect(cHeaderFieldNames pOther) => new cHeaderFieldNames(mFieldNames.Intersect(pOther.mFieldNames));
        public cHeaderFieldNames Except(cHeaderFieldNames pOther) => new cHeaderFieldNames(mFieldNames.Except(pOther.mFieldNames));

        public string this[int pIndex] => mFieldNames[pIndex];
        public int Count => mFieldNames.Count;
        public IEnumerator<string> GetEnumerator() => mFieldNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object pObject) => this == pObject as cHeaderFieldNames;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (string lFieldName in mFieldNames) lHash = lHash * 23 + lFieldName.GetHashCode();
                return lHash;
            }
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHeaderFieldNames));
            foreach (var lFieldName in mFieldNames) lBuilder.Append(lFieldName);
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

        public static cHeaderFieldNames operator |(cHeaderFieldNames pNames, string pFieldName)
        {
            if (pNames == null) return null;
            string lFieldName = pFieldName.ToUpperInvariant();
            if (pNames.mFieldNames.Contains(lFieldName)) return pNames;

            List<string> lFieldNames = new List<string>(pNames.mFieldNames);
            lFieldNames.Add(lFieldName);
            lFieldNames.Sort();

            return new cHeaderFieldNames(lFieldNames.AsReadOnly());
        }

        public static bool TryConstruct(List<string> pFieldNames, out cHeaderFieldNames rNames)
        {
            if (ZTryNormaliseFieldNames(pFieldNames, out var lFieldNames))
            {
                rNames = new cHeaderFieldNames(lFieldNames);
                return true;
            }

            rNames = null;
            return false;
        }

        private static bool ZTryNormaliseFieldNames(IEnumerable<string> pFieldNames, out ReadOnlyCollection<string> rNormalisedFieldNames)
        {
            if (pFieldNames == null) throw new ArgumentNullException(nameof(pFieldNames));

            List<string> lUpperValidNames = new List<string>();

            foreach (var lName in pFieldNames)
            {
                if (string.IsNullOrEmpty(lName)) { rNormalisedFieldNames = null; return false; }
                foreach (char lChar in lName) if (!cCharset.FText.Contains(lChar)) { rNormalisedFieldNames = null; return false; }
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

            rNormalisedFieldNames = lDistinctSortedNames.AsReadOnly();
            return true;
        }
    }
}