using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cSectionCacheMailboxId : IEquatable<cSectionCacheMailboxId>
    {
        public readonly cAccountId AccountId;
        public readonly cMailboxName MailboxName;

        internal cSectionCacheMailboxId(iMailboxHandle pMailboxHandle)
        {
            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            AccountId = pMailboxHandle.MailboxCache.AccountId;
            MailboxName = pMailboxHandle.MailboxName;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCacheMailboxId pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionCacheMailboxId;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + AccountId.GetHashCode();
                lHash = lHash * 23 + MailboxName.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionCacheMailboxId)}({AccountId},{MailboxName})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCacheMailboxId pA, cSectionCacheMailboxId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.AccountId == pB.AccountId && pA.MailboxName == pB.MailboxName;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCacheMailboxId pA, cSectionCacheMailboxId pB) => !(pA == pB);
    }
}