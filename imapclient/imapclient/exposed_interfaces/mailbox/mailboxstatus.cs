using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailboxStatus
    {
        public readonly int MessageCount;
        public readonly int Recent;
        public readonly uint UIDNext;
        public readonly int NewUnknownUID; // the number of new messages in the mailbox for which we don't know the UID (indicates the inaccuracy of UIDNext)
        public readonly uint UIDValidity;
        public readonly int Unseen;
        public readonly int UnseenUnknown; // the number of messages in the mailbox for which we don't know if they are seen or unseen (indicates the inaccuracy of Unseen)
        public readonly ulong HighestModSeq;

        public cMailboxStatus(int pMessageCount, int pRecent, uint pUIDNext, int pNewKnownUID, uint pUIDValidity, int pUnseen, int pUnseenUnknown, ulong pHighestModSeq)
        {
            MessageCount = pMessageCount;
            Recent = pRecent;
            UIDNext = pUIDNext;
            NewUnknownUID = pNewKnownUID;
            UIDValidity = pUIDValidity;
            Unseen = pUnseen;
            UnseenUnknown = pUnseenUnknown;
            HighestModSeq = pHighestModSeq;
        }

        public override bool Equals(object pObject) => this == pObject as cMailboxStatus;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + MessageCount.GetHashCode();
                lHash = lHash * 23 + Recent.GetHashCode();
                lHash = lHash * 23 + UIDNext.GetHashCode();
                lHash = lHash * 23 + NewUnknownUID.GetHashCode();
                lHash = lHash * 23 + UIDValidity.GetHashCode();
                lHash = lHash * 23 + Unseen.GetHashCode();
                lHash = lHash * 23 + UnseenUnknown.GetHashCode();
                lHash = lHash * 23 + HighestModSeq.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cMailboxStatus)}({MessageCount},{Recent},{UIDNext},{NewUnknownUID},{UIDValidity},{Unseen},{UnseenUnknown},{HighestModSeq})";

        public static fMailboxCacheItemDifferences Differences(cMailboxStatus pA, cMailboxStatus pB)
        {
            if (ReferenceEquals(pA, pB)) return 0;
            if (ReferenceEquals(pA, null)) return fMailboxCacheItemDifferences.all;
            if (ReferenceEquals(pB, null)) return fMailboxCacheItemDifferences.all;

            fMailboxCacheItemDifferences lResult = 0;

            if (pA.MessageCount != pB.MessageCount) lResult |= fMailboxCacheItemDifferences.messagecount;
            if (pA.Recent != pB.Recent) lResult |= fMailboxCacheItemDifferences.recent;
            if (pA.UIDNext != pB.UIDNext) lResult |= fMailboxCacheItemDifferences.uidnext;
            if (pA.NewUnknownUID != pB.NewUnknownUID) lResult |= fMailboxCacheItemDifferences.newunknownuid;
            if (pA.UIDValidity != pB.UIDValidity) lResult |= fMailboxCacheItemDifferences.uidvalidity;
            if (pA.Unseen != pB.Unseen) lResult |= fMailboxCacheItemDifferences.unseen;
            if (pA.UnseenUnknown != pB.UnseenUnknown) lResult |= fMailboxCacheItemDifferences.unseenunknown;
            if (pA.HighestModSeq != pB.HighestModSeq) lResult |= fMailboxCacheItemDifferences.highestmodseq;

            return lResult;
        }

        public static bool operator ==(cMailboxStatus pA, cMailboxStatus pB) => Differences(pA, pB) == 0;
        public static bool operator !=(cMailboxStatus pA, cMailboxStatus pB) => !(pA == pB);
    }
}