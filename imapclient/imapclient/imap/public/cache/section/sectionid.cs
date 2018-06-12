using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cSectionId : IEquatable<cSectionId>
    {
        public readonly cMessageUID MessageUID;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        internal cSectionId(cMessageUID pMessageUID, cSection pSection, eDecodingRequired pDecoding)
        {
            MessageUID = pMessageUID ?? throw new ArgumentNullException(nameof(pMessageUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionId pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        internal bool Equals(cSectionHandle pObject)
        {
            if (pObject == null || pObject.MessageHandle.UID == null) return false;
            return MessageUID.MailboxId == pObject.MessageHandle.MessageCache.MailboxHandle.MailboxId && MessageUID.UID == pObject.MessageHandle.UID && Section == pObject.Section && Decoding == pObject.Decoding;
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionId;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MessageUID.GetHashCode();
                lHash = lHash * 23 + Section.GetHashCode();
                lHash = lHash * 23 + Decoding.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionId)}({MessageUID},{Section},{Decoding})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionId pA, cSectionId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MessageUID == pB.MessageUID && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionId pA, cSectionId pB) => !(pA == pB);
    }
}