using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cMailboxStatus
    {
        public enum fProperties
        {
            messagecount = 1 << 0,
            recentcount = 1 << 1,
            uidnext = 1 << 2,
            newunknownuidcount = 1 << 3,
            uidvalidity = 1 << 4,
            unseencount = 1 << 5,
            unseenunknowncount = 1 << 6,
            highestmodseq = 1 << 7
        }

        public static readonly cMailboxStatus NonExistent = new cMailboxStatus(0, 0, 0, 0, 0, 0, 0, 0);

        private static int mLastSequence = 0;

        public readonly int Sequence;
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
            Sequence = Interlocked.Increment(ref mLastSequence);
            MessageCount = pMessageCount;
            RecentCount = pRecentCount;
            UIDNext = pUIDNext;
            NewUnknownUIDCount = pNewKnownUIDCount;
            UIDValidity = pUIDValidity;
            UnseenCount = pUnseenCount;
            UnseenUnknownCount = pUnseenUnknownCount;
            HighestModSeq = pHighestModSeq;
        }

        public override string ToString() => $"{nameof(cMailboxStatus)}({MessageCount},{RecentCount},{UIDNext},{NewUnknownUIDCount},{UIDValidity},{UnseenCount},{UnseenUnknownCount},{HighestModSeq})";

        public static int LastSequence = mLastSequence;

        public static fProperties Differences(cMailboxStatus pOld, cMailboxStatus pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (ReferenceEquals(pOld, NonExistent)) return 0;

            fProperties lProperties = 0;

            if (pOld.MessageCount != pNew.MessageCount) lProperties |= fProperties.messagecount;
            if (pOld.RecentCount != pNew.RecentCount) lProperties |= fProperties.messagecount;
            if (pOld.UIDNext != pNew.UIDNext) lProperties |= fProperties.messagecount;
            if (pOld.NewUnknownUIDCount != pNew.NewUnknownUIDCount) lProperties |= fProperties.messagecount;
            if (pOld.UIDValidity != pNew.UIDValidity) lProperties |= fProperties.messagecount;
            if (pOld.UnseenCount != pNew.UnseenCount) lProperties |= fProperties.messagecount;
            if (pOld.UnseenUnknownCount != pNew.UnseenUnknownCount) lProperties |= fProperties.messagecount;
            if (pOld.HighestModSeq != pNew.HighestModSeq) lProperties |= fProperties.messagecount;

            return lProperties;
        }
    }
}