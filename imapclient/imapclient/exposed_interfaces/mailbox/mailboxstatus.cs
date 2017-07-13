﻿using System;
using System.Threading;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailboxStatus
    {
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

        public static fMailboxProperties Differences(cMailboxStatus pOld, cMailboxStatus pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (pOld == null) return 0;

            fMailboxProperties lProperties = 0;

            if (pOld.MessageCount != pNew.MessageCount) lProperties |= fMailboxProperties.messagecount;
            if (pOld.RecentCount != pNew.RecentCount) lProperties |= fMailboxProperties.messagecount;
            if (pOld.UIDNext != pNew.UIDNext) lProperties |= fMailboxProperties.messagecount;
            if (pOld.NewUnknownUIDCount != pNew.NewUnknownUIDCount) lProperties |= fMailboxProperties.messagecount;
            if (pOld.UIDValidity != pNew.UIDValidity) lProperties |= fMailboxProperties.messagecount;
            if (pOld.UnseenCount != pNew.UnseenCount) lProperties |= fMailboxProperties.messagecount;
            if (pOld.UnseenUnknownCount != pNew.UnseenUnknownCount) lProperties |= fMailboxProperties.messagecount;
            if (pOld.HighestModSeq != pNew.HighestModSeq) lProperties |= fMailboxProperties.messagecount;

            if (lProperties != 0) lProperties |= fMailboxProperties.mailboxstatus;

            return lProperties;
        }
    }
}