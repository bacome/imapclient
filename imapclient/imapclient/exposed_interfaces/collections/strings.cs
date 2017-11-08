using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    public class cStrings : ReadOnlyCollection<string>
    {
        public cStrings(IList<string> pStrings) : base(pStrings) { }

        public override bool Equals(object pObject) => this == pObject as cStrings;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (string lString in this) if (lString == null) lHash = lHash * 23; else lHash = lHash * 23 + lString.GetHashCode();
                return lHash;
            }
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStrings));
            foreach (var lString in this) lBuilder.Append(lString);
            return lBuilder.ToString();
        }

        public static bool operator ==(cStrings pA, cStrings pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            if (pA.Count != pB.Count) return false;
            for (int i = 0; i < pA.Count; i++) if (pA[i] != pB[i]) return false;
            return true;
        }

        public static bool operator !=(cStrings pA, cStrings pB) => !(pA == pB);
    }
}