using System;

namespace work.bacome.imapclient
{
    public class cMailboxId
    {
        public readonly cAccountId AccountId;
        public readonly cMailboxName MailboxName;

        public cMailboxId(cAccountId pAccountId, cMailboxName pMailboxName)
        {
            AccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
            MailboxName = pMailboxName ?? throw new ArgumentNullException(nameof(pMailboxName));
        }

        public override bool Equals(object pObject) => this == pObject as cMailboxId;

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

        public override string ToString() => $"{nameof(cMailboxId)}({AccountId},{MailboxName})";

        public static bool operator ==(cMailboxId pA, cMailboxId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.AccountId == pB.AccountId && pA.MailboxName == pB.MailboxName);
        }

        public static bool operator !=(cMailboxId pA, cMailboxId pB) => !(pA == pB);
    }
}