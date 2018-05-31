using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cSectionCachePersistentKey : IEquatable<cSectionCachePersistentKey>
    {
        private readonly iMailboxHandle mMailboxHandle;
        public readonly cSectionCacheMessageId MessageId;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        internal cSectionCachePersistentKey(iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding)
        {
            MessageId = new cSectionCacheMessageId(pMailboxHandle, pUID);
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;
        }

        internal cSectionCachePersistentKey(cSectionCacheNonPersistentKey pKey)
        {
            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pKey.MessageHandle.UID == null) throw new ArgumentOutOfRangeException(nameof(pKey));
            MessageId = new cSectionCacheMessageId(pKey.MessageHandle);
            Section = pKey.Section;
            Decoding = pKey.Decoding;
        }

        internal bool UIDNotSticky => mMailboxHandle.SelectedProperties.UIDNotSticky ?? true;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCachePersistentKey pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        internal bool Equals(cSectionCacheNonPersistentKey pObject)
        {
            if (pObject == null || pObject.MessageHandle.UID == null) return false;
            return MessageId.MailboxId == pObject.MailboxId && MessageId.UID == pObject.MessageHandle.UID && Section == pObject.Section && Decoding == pObject.Decoding;
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionCachePersistentKey;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MessageId.GetHashCode();
                lHash = lHash * 23 + Section.GetHashCode();
                lHash = lHash * 23 + Decoding.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionCachePersistentKey)}({MessageId},{Section},{Decoding})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCachePersistentKey pA, cSectionCachePersistentKey pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MessageId == pB.MessageId && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCachePersistentKey pA, cSectionCachePersistentKey pB) => !(pA == pB);
    }
}