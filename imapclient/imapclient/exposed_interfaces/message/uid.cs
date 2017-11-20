using System;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// An IMAP message UID
    /// </summary>
    /// <seealso cref="cMessage.UID"/>
    /// <seealso cref="cMessage.Copy(cMailbox)"/>
    /// <seealso cref="cFilter.UID"/>
    /// <seealso cref="iMessageHandle.UID"/>
    /// <seealso cref="cMailbox.Copy(System.Collections.Generic.IEnumerable{cMessage})"/>
    /// <seealso cref="cMailbox.Message(cUID, cMessageCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.UIDFetch(cUID, cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMailbox.UIDCopy(cUID, cMailbox)"/>
    /// <seealso cref="cMailbox.UIDCopy(System.Collections.Generic.IEnumerable{cUID}, cMailbox)"/>
    /// <seealso cref="cMailbox.UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>
    /// <seealso cref="cMailbox.UIDStore(System.Collections.Generic.IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>
    public class cUID : IComparable<cUID>, IEquatable<cUID>
    {
        /**<summary>The UIDValidity.</summary>*/
        public readonly uint UIDValidity;
        /**<summary>The UID.</summary>*/
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo(object)"/>
        public int CompareTo(cUID pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo = UIDValidity.CompareTo(pOther.UIDValidity);
            if (lCompareTo == 0) return UID.CompareTo(pOther.UID);
            return lCompareTo;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cUID pOther) => this == pOther;

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
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.UIDValidity == pB.UIDValidity && pA.UID == pB.UID);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cUID pA, cUID pB) => !(pA == pB);
    }
}