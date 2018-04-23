using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cSectionCachePersistentKey : IEquatable<cSectionCachePersistentKey>, IEquatable<cSectionCacheNonPersistentKey>
    {
        public readonly cAccountId AccountId;
        public readonly cMailboxName MailboxName;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        internal cSectionCachePersistentKey(cAccountId pAccountId, cMailboxName pMailboxName, cUID pUID, cSection pSection, eDecodingRequired pDecoding)
        {
            AccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
            MailboxName = pMailboxName ?? throw new ArgumentNullException(nameof(pMailboxName));
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;
        }


        internal cSectionCachePersistentKey(cSectionCacheNonPersistentKey pKey)
        {
            if (ReferenceEquals(pKey, null)) throw new ArgumentNullException(nameof(pKey));
            if (pKey.UID == null) throw new ArgumentOutOfRangeException(nameof(pKey));

            AccountId = pKey.AccountId;
            MailboxName = pKey.MailboxName;
            UID = pKey.UID;
            Section = pKey.Section;
            Decoding = pKey.Decoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCachePersistentKey pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCacheNonPersistentKey pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionCachePersistentKey;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + AccountId.GetHashCode();
                lHash = lHash * 23 + MailboxName.GetHashCode();
                lHash = lHash * 23 + UID.GetHashCode();
                lHash = lHash * 23 + Section.GetHashCode();
                lHash = lHash * 23 + Decoding.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionCachePersistentKey)}({AccountId},{MailboxName},{UID},{Section},{Decoding})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCachePersistentKey pA, cSectionCachePersistentKey pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.AccountId == pB.AccountId && pA.MailboxName == pB.MailboxName && pA.UID == pB.UID && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCachePersistentKey pA, cSectionCachePersistentKey pB) => !(pA == pB);

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCachePersistentKey pA, cSectionCacheNonPersistentKey pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.AccountId == pB.AccountId && pA.MailboxName == pB.MailboxName && pA.UID == pB.UID && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCachePersistentKey pA, cSectionCacheNonPersistentKey pB) => !(pA == pB);
    }

    public class cSectionCacheNonPersistentKey : IEquatable<cSectionCacheNonPersistentKey>, IEquatable<cSectionCachePersistentKey>
    {
        public readonly cAccountId AccountId;
        public readonly cIMAPMessage Message;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        internal cSectionCacheNonPersistentKey(cAccountId pAccountId, cIMAPMessage pMessage, cSection pSection, eDecodingRequired pDecoding)
        {
            AccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;
        }

        public cMailboxName MailboxName => Message.MessageHandle.MessageCache.MailboxHandle.MailboxName;
        public cUID UID => Message.MessageHandle.UID;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCacheNonPersistentKey pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCachePersistentKey pObject) => pObject == this;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionCacheNonPersistentKey;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + AccountId.GetHashCode();
                lHash = lHash * 23 + Message.GetHashCode();
                lHash = lHash * 23 + Section.GetHashCode();
                lHash = lHash * 23 + Decoding.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionCacheNonPersistentKey)}({AccountId},{Message},{Section},{Decoding})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCacheNonPersistentKey pA, cSectionCacheNonPersistentKey pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.AccountId == pB.AccountId && pA.Message == pB.Message && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCacheNonPersistentKey pA, cSectionCacheNonPersistentKey pB) => !(pA == pB);

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCacheNonPersistentKey pA, cSectionCachePersistentKey pB) => pB == pA;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCacheNonPersistentKey pA, cSectionCachePersistentKey pB) => pB != pA;
    }
}