using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// An IMAP message UID
    /// </summary>
    public class cUID : IComparable<cUID>, IEquatable<cUID>
    {
        /**<summary>The UIDValidity.</summary>*/
        public readonly uint UIDValidity;
        /**<summary>The UID.</summary>*/
        public readonly uint UID;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pUIDValidity"></param>
        /// <param name="pUID"></param>
        public cUID(uint pUIDValidity, uint pUID)
        {
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            if (pUID == 0) throw new ArgumentOutOfRangeException(nameof(pUID));
            UIDValidity = pUIDValidity;
            UID = pUID;
        }

        /// <summary>
        /// Compares this instance with the specified <see cref="cUID"/> object.
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public int CompareTo(cUID pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo = UIDValidity.CompareTo(pOther.UIDValidity);
            if (lCompareTo == 0) return UID.CompareTo(pOther.UID);
            return lCompareTo;
        }

        /// <summary>
        /// Determines whether this instance and the specified object have the same value.
        /// </summary>
        /// <param name="pOther"></param>
        /// <returns></returns>
        public bool Equals(cUID pOther) => this == pOther;

        /// <summary>
        /// Determines whether this instance and the specified object have the same value.
        /// </summary>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public override bool Equals(object pObject) => this == pObject as cUID;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
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

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString() => $"{nameof(cUID)}({UIDValidity},{UID})";

        /// <summary>
        /// Determines whether two instances have the same value.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator ==(cUID pA, cUID pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.UIDValidity == pB.UIDValidity && pA.UID == pB.UID);
        }

        /// <summary>
        /// Determines whether two instances have different values.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator !=(cUID pA, cUID pB) => !(pA == pB);
    }
}