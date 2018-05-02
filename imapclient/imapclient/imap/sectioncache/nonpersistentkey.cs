using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public partial class cSectionCache
    {
        internal class cNonPersistentKey : IEquatable<cNonPersistentKey>
        {
            public readonly cAccountId AccountId;
            public readonly cIMAPMessage Message;
            public readonly cSection Section;
            public readonly eDecodingRequired Decoding;

            internal cNonPersistentKey(cAccountId pAccountId, cIMAPMessage pMessage, cSection pSection, eDecodingRequired pDecoding)
            {
                AccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
                Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
                Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
                Decoding = pDecoding;
            }

            public cMailboxName MailboxName => Message.MessageHandle.MessageCache.MailboxHandle.MailboxName;
            public cUID UID => Message.MessageHandle.UID;

            /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
            public bool Equals(cNonPersistentKey pObject) => this == pObject;

            /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
            public bool Equals(cSectionCachePersistentKey pObject)
            {
                if (pObject == null) return false;
                return pObject.Equals(this);
            }

            /// <inheritdoc />
            public override bool Equals(object pObject) => this == pObject as cNonPersistentKey;

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
            public override string ToString() => $"{nameof(cNonPersistentKey)}({AccountId},{Message},{Section},{Decoding})";

            /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
            public static bool operator ==(cNonPersistentKey pA, cNonPersistentKey pB)
            {
                if (ReferenceEquals(pA, pB)) return true;
                if (ReferenceEquals(pA, null)) return false;
                if (ReferenceEquals(pB, null)) return false;
                return pA.AccountId == pB.AccountId && pA.Message == pB.Message && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
            }

            /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
            public static bool operator !=(cNonPersistentKey pA, cNonPersistentKey pB) => !(pA == pB);
        }

    }
}
