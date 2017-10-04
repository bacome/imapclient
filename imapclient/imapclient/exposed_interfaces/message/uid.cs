using System;

namespace work.bacome.imapclient
{
    public class cUID : IComparable<cUID>, IEquatable<cUID>
    {
        public readonly uint UIDValidity;
        public readonly uint UID;

        public cUID(uint pUIDValidity, uint pUID)
        {
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            if (pUID == 0) throw new ArgumentOutOfRangeException(nameof(pUID));
            UIDValidity = pUIDValidity;
            UID = pUID;
        }

        public int CompareTo(cUID pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo = UIDValidity.CompareTo(pOther.UIDValidity);
            if (lCompareTo == 0) return UID.CompareTo(pOther.UID);
            return lCompareTo;
        }

        public bool Equals(cUID pOther) => this == pOther;

        public override bool Equals(object pObject) => this == pObject as cUID;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + UIDValidity.GetHashCode();
                lHash = lHash * 23 + UID.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cUID)}({UIDValidity},{UID})";

        public static bool operator ==(cUID pA, cUID pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.UIDValidity == pB.UIDValidity && pA.UID == pB.UID);
        }

        public static bool operator !=(cUID pA, cUID pB) => !(pA == pB);
    }
}