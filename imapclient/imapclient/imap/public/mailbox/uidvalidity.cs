using System;
using System.Runtime.Serialization;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP message UIDValidity
    /// </summary>
    [Serializable]
    public struct sUIDValidity : IEquatable<sUIDValidity>, IComparable<sUIDValidity>
    {
        public static readonly sUIDValidity None = new sUIDValidity();

        /**<summary>The UIDValidity of the instance.</summary>*/
        public readonly uint UIDValidity;
        public readonly bool IsSticky;

        public sUIDValidity(uint pUIDValidity, bool pIsSticky)
        {
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            UIDValidity = pUIDValidity;
            IsSticky = pIsSticky;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (UIDValidity == 0 && IsSticky) throw new cDeserialiseException(nameof(sUIDValidity), nameof(IsSticky), kDeserialiseExceptionMessage.IsInconsistent);
        }

        public bool IsNone => UIDValidity == 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(sUIDValidity pOther) => this == pOther;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo(object)"/>
        public int CompareTo(sUIDValidity pOther)
        {
            int lCompareTo = UIDValidity.CompareTo(pOther.UIDValidity);
            if (lCompareTo == 0) return IsSticky.CompareTo(pOther.IsSticky);
            return lCompareTo;
        }

        /// <inheritdoc/>
        public override bool Equals(object pObject) => (pObject is sUIDValidity) ? this == (sUIDValidity)pObject : false;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + UIDValidity.GetHashCode();
                lHash = lHash * 23 + IsSticky.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(sUIDValidity)}({UIDValidity},{IsSticky})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(sUIDValidity pA, sUIDValidity pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.UIDValidity == pB.UIDValidity && pA.IsSticky == pB.IsSticky;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(sUIDValidity pA, sUIDValidity pB) => !(pA == pB);
    }
}