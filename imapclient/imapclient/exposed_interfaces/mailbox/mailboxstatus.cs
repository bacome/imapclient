using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailboxStatus
    {
        public readonly int MessageCount;
        public readonly int RecentCount;
        public readonly uint UIDNext;
        public readonly int NewUnknownUIDCount; // the number of new messages in the mailbox for which we don't know the UID (indicates the inaccuracy of UIDNext)
        public readonly uint UIDValidity;
        public readonly int UnseenCount;
        public readonly int UnseenUnknownCount; // the number of messages in the mailbox for which we don't know if they are seen or unseen (indicates the inaccuracy of Unseen)
        public readonly ulong HighestModSeq;

        public cMailboxStatus(int pMessageCount, int pRecentCount, uint pUIDNext, int pNewKnownUIDCount, uint pUIDValidity, int pUnseenCount, int pUnseenUnknownCount, ulong pHighestModSeq)
        {
            MessageCount = pMessageCount;
            RecentCount = pRecentCount;
            UIDNext = pUIDNext;
            NewUnknownUIDCount = pNewKnownUIDCount;
            UIDValidity = pUIDValidity;
            UnseenCount = pUnseenCount;
            UnseenUnknownCount = pUnseenUnknownCount;
            HighestModSeq = pHighestModSeq;
        }

        public override bool Equals(object pObject) => this == pObject as cMailboxStatus;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + MessageCount.GetHashCode();
                lHash = lHash * 23 + RecentCount.GetHashCode();
                lHash = lHash * 23 + UIDNext.GetHashCode();
                lHash = lHash * 23 + NewUnknownUIDCount.GetHashCode();
                lHash = lHash * 23 + UIDValidity.GetHashCode();
                lHash = lHash * 23 + UnseenCount.GetHashCode();
                lHash = lHash * 23 + UnseenUnknownCount.GetHashCode();
                lHash = lHash * 23 + HighestModSeq.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cMailboxStatus)}({MessageCount},{RecentCount},{UIDNext},{NewUnknownUIDCount},{UIDValidity},{UnseenCount},{UnseenUnknownCount},{HighestModSeq})";

        public static fMailboxCacheItemDifferences Differences(cMailboxStatus pA, cMailboxStatus pB)
        {
            if (ReferenceEquals(pA, pB)) return 0;
            if (ReferenceEquals(pA, null)) return fMailboxCacheItemDifferences.allstatus;
            if (ReferenceEquals(pB, null)) return fMailboxCacheItemDifferences.allstatus;

            fMailboxCacheItemDifferences lResult = 0;

            if (pA.MessageCount != pB.MessageCount) lResult |= fMailboxCacheItemDifferences.messagecount;
            if (pA.RecentCount != pB.RecentCount) lResult |= fMailboxCacheItemDifferences.recentcount;
            if (pA.UIDNext != pB.UIDNext) lResult |= fMailboxCacheItemDifferences.uidnext;
            if (pA.NewUnknownUIDCount != pB.NewUnknownUIDCount) lResult |= fMailboxCacheItemDifferences.newunknownuidcount;
            if (pA.UIDValidity != pB.UIDValidity) lResult |= fMailboxCacheItemDifferences.uidvalidity;
            if (pA.UnseenCount != pB.UnseenCount) lResult |= fMailboxCacheItemDifferences.unseencount;
            if (pA.UnseenUnknownCount != pB.UnseenUnknownCount) lResult |= fMailboxCacheItemDifferences.unseenunknowncount;
            if (pA.HighestModSeq != pB.HighestModSeq) lResult |= fMailboxCacheItemDifferences.highestmodseq;

            return lResult;
        }

        public static bool operator ==(cMailboxStatus pA, cMailboxStatus pB) => Differences(pA, pB) == 0;
        public static bool operator !=(cMailboxStatus pA, cMailboxStatus pB) => Differences(pA, pB) != 0;
    }
}