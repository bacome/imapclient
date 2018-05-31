using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheMessageId : IEquatable<cSectionCacheMessageId>
    {
        public readonly cSectionCacheMailboxId MailboxId;
        public readonly cUID UID;

        internal cSectionCacheMessageId(iMessageHandle pMessageHandle)
        {
            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            MailboxId = new cSectionCacheMailboxId(pMessageHandle.MessageCache.MailboxHandle);
            UID = pMessageHandle.UID ?? throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
        }

        internal cSectionCacheMessageId(iMailboxHandle pMailboxHandle, cUID pUID)
        {
            MailboxId = new cSectionCacheMailboxId(pMailboxHandle);
            UID = pUID ?? throw new ArgumentOutOfRangeException(nameof(pUID));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCacheMessageId pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        internal bool Equals(cSectionCacheNonPersistentKey pObject)
        {
            if (pObject == null) return false;
            return MailboxId == pObject.MailboxId && UID == pObject.MessageHandle.UID;
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionCacheMessageId;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MailboxId.GetHashCode();
                lHash = lHash * 23 + UID.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionCacheMessageId)}({MailboxId},{UID})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCacheMessageId pA, cSectionCacheMessageId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MailboxId == pB.MailboxId && pA.UID == pB.UID;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCacheMessageId pA, cSectionCacheMessageId pB) => !(pA == pB);
    }
}