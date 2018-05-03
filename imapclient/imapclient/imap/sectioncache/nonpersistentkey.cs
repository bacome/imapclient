using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public partial class cSectionCache
    {
        internal class cNonPersistentKey : IEquatable<cNonPersistentKey>
        {
            public readonly iMessageHandle MessageHandle;
            public readonly cSection Section;
            public readonly eDecodingRequired Decoding;

            internal cNonPersistentKey(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding)
            {
                MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
                Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
                Decoding = pDecoding;
            }

            public cAccountId AccountId => MessageHandle.MessageCache.MailboxHandle.MailboxCache.AccountId;
            public iMailboxHandle MailboxHandle => MessageHandle.MessageCache.MailboxHandle;
            public cMailboxName MailboxName => MessageHandle.MessageCache.MailboxHandle.MailboxName;
            public cUID UID => MessageHandle.UID;

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

                    lHash = lHash * 23 + MessageHandle.GetHashCode();
                    lHash = lHash * 23 + Section.GetHashCode();
                    lHash = lHash * 23 + Decoding.GetHashCode();

                    return lHash;
                }
            }

            /// <inheritdoc />
            public override string ToString() => $"{nameof(cNonPersistentKey)}({MessageHandle},{Section},{Decoding})";

            /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
            public static bool operator ==(cNonPersistentKey pA, cNonPersistentKey pB)
            {
                if (ReferenceEquals(pA, pB)) return true;
                if (ReferenceEquals(pA, null)) return false;
                if (ReferenceEquals(pB, null)) return false;
                return pA.MessageHandle == pB.MessageHandle && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
            }

            /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
            public static bool operator !=(cNonPersistentKey pA, cNonPersistentKey pB) => !(pA == pB);
        }
    }
}
