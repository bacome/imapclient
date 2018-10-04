using System;
using System.Runtime.Serialization;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP message UIDValidity
    /// </summary>
    [Serializable]
    public class cUIDValidity : IEquatable<cUIDValidity>, IComparable<cUIDValidity>
    {
        public static cUIDValidity None = new cUIDValidity();

        /**<summary>The UIDValidity of the instance.</summary>*/
        public readonly uint UIDValidity;
        public readonly bool UIDNotSticky;

        private cUIDValidity()
        {
            UIDValidity = 0;
            UIDNotSticky = true;
        }

        public cUIDValidity(uint pUIDValidity, bool pUIDNotSticky)
        {
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            UIDValidity = pUIDValidity;
            UIDNotSticky = pUIDNotSticky;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (UIDValidity == 0 && !UIDNotSticky) throw new cDeserialiseException(nameof(cUIDValidity), nameof(UIDNotSticky), kDeserialiseExceptionMessage.IsInconsistent);
        }

        public bool IsNone => UIDValidity == 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cUIDValidity pOther) => this == pOther;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo(object)"/>
        public int CompareTo(cUIDValidity pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo = UIDValidity.CompareTo(pOther.UIDValidity);
            if (lCompareTo == 0) return UIDNotSticky.CompareTo(pOther.UIDNotSticky);
            return lCompareTo;
        }

        /// <inheritdoc/>
        public override bool Equals(object pObject) => this == pObject as cUIDValidity;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + UIDValidity.GetHashCode();
                lHash = lHash * 23 + UIDNotSticky.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cUIDValidity)}({UIDValidity},{UIDNotSticky})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cUIDValidity pA, cUIDValidity pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.UIDValidity == pB.UIDValidity && pA.UIDNotSticky == pB.UIDNotSticky;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cUIDValidity pA, cUIDValidity pB) => !(pA == pB);
    }
}