using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cMailboxId : IEquatable<cMailboxId>
    {
        public readonly cAccountId AccountId;
        public readonly cMailboxName MailboxName;

        public cMailboxId(cAccountId pAccountId, cMailboxName pMailboxName)
        {
            AccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
            MailboxName = pMailboxName ?? throw new ArgumentNullException(nameof(pMailboxName));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMailboxId pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cMailboxId;

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
        public override string ToString() => $"{nameof(cMailboxId)}({AccountId},{MailboxName})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMailboxId pA, cMailboxId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.AccountId == pB.AccountId && pA.MailboxName == pB.MailboxName;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMailboxId pA, cMailboxId pB) => !(pA == pB);
    }
}