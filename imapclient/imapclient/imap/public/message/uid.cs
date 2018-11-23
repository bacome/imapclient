using System;
using System.Runtime.Serialization;
using work.bacome.imapinternals;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP message UID
    /// </summary>
    [Serializable]
    public class cUID : IEquatable<cUID>, IComparable<cUID>
    {
        /**<summary>The UIDValidity of the instance.</summary>*/
        public readonly uint UIDValidity;

        /**<summary>The UID of the instance.</summary>*/
        public readonly uint UID;

        /// <summary>
        /// Initialises a new instance with the specified UIDValidity and UID.
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

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (UIDValidity == 0) throw new cDeserialiseException(nameof(cUID), nameof(UIDValidity), kDeserialiseExceptionMessage.IsInvalid);
            if (UID == 0) throw new cDeserialiseException(nameof(cUID), nameof(UID), kDeserialiseExceptionMessage.IsInvalid);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cUID pOther) => this == pOther;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo(object)"/>
        public int CompareTo(cUID pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo = UIDValidity.CompareTo(pOther.UIDValidity);
            if (lCompareTo != 0) return lCompareTo;
            return UID.CompareTo(pOther.UID);
        }

        /// <inheritdoc/>
        public override bool Equals(object pObject) => this == pObject as cUID;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
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

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cUID)}({UIDValidity},{UID})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cUID pA, cUID pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.UIDValidity == pB.UIDValidity && pA.UID == pB.UID;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cUID pA, cUID pB) => !(pA == pB);

        public static bool operator <(cUID pA, cUID pB)
        {
            if (ReferenceEquals(pA, pB)) return false;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            if (pA.UIDValidity != pB.UIDValidity) throw new InvalidOperationException();
            return pA.UID < pB.UID;
        }

        public static bool operator >(cUID pA, cUID pB)
        {
            if (ReferenceEquals(pA, pB)) return false;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            if (pA.UIDValidity != pB.UIDValidity) throw new InvalidOperationException();
            return pA.UID > pB.UID;
        }

        public static bool operator <=(cUID pA, cUID pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            if (pA.UIDValidity != pB.UIDValidity) throw new InvalidOperationException();
            return pA.UID <= pB.UID;
        }

        public static bool operator >=(cUID pA, cUID pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            if (pA.UIDValidity != pB.UIDValidity) throw new InvalidOperationException();
            return pA.UID >= pB.UID;
        }
    }
}