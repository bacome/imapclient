using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailboxStatus
    {
        public readonly int MessageCount;
        public readonly int RecentCount;
        public readonly uint UIDNext;
        public readonly int UIDNextUnknownCount; // the number of messages in the mailbox for which we don't know the UID (indicates the inaccuracy of UIDNext)
        public readonly uint UIDValidity;
        public readonly int UnseenCount;
        public readonly int UnseenUnknownCount; // the number of messages in the mailbox for which we don't know the unseen (indicates the inaccuracy of Unseen)
        public readonly ulong HighestModSeq;

        public cMailboxStatus(uint pMessages, uint pRecent, uint pUIDNext, uint pUIDValidity, uint pUnseen, ulong pHighestModSeq)
        {
            MessageCount = (int)pMessages;
            RecentCount = (int)pRecent;
            UIDNext = pUIDNext;
            UIDNextUnknownCount = 0;
            UIDValidity = pUIDValidity;
            UnseenCount = (int)pUnseen;
            UnseenUnknownCount = 0;
            HighestModSeq = pHighestModSeq;
        }

        public cMailboxStatus(int pMessageCount, int pRecentCount, uint pUIDNext, int pUIDNextUnknownCount, uint pUIDValidity, int pUnseenCount, int pUnseenUnknownCount, ulong pHighestModSeq)
        {
            MessageCount = pMessageCount;
            RecentCount = pRecentCount;
            UIDNext = pUIDNext;
            UIDNextUnknownCount = pUIDNextUnknownCount;
            UIDValidity = pUIDValidity;
            UnseenCount = pUnseenCount;
            UnseenUnknownCount = pUnseenUnknownCount;
            HighestModSeq = pHighestModSeq;
        }

        public override string ToString() => $"{nameof(cMailboxStatus)}({MessageCount},{RecentCount},{UIDNext},{UIDNextUnknownCount},{UIDValidity},{UnseenCount},{UnseenUnknownCount},{HighestModSeq})";

        public static fMailboxProperties Differences(cMailboxStatus pOld, cMailboxStatus pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (pOld == null) return 0;

            fMailboxProperties lProperties = 0;

            if (pOld.MessageCount != pNew.MessageCount) lProperties |= fMailboxProperties.messagecount;
            if (pOld.RecentCount != pNew.RecentCount) lProperties |= fMailboxProperties.recentcount;
            if (pOld.UIDNext != pNew.UIDNext) lProperties |= fMailboxProperties.uidnext;
            if (pOld.UIDNextUnknownCount != pNew.UIDNextUnknownCount) lProperties |= fMailboxProperties.uidnextunknowncount;
            if (pOld.UIDValidity != pNew.UIDValidity) lProperties |= fMailboxProperties.uidvalidity;
            if (pOld.UnseenCount != pNew.UnseenCount) lProperties |= fMailboxProperties.unseencount;
            if (pOld.UnseenUnknownCount != pNew.UnseenUnknownCount) lProperties |= fMailboxProperties.unseenunknowncount;
            if (pOld.HighestModSeq != pNew.HighestModSeq) lProperties |= fMailboxProperties.highestmodseq;

            if (lProperties != 0) lProperties |= fMailboxProperties.status;

            return lProperties;
        }
    }
}