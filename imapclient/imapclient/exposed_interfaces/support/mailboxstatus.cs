using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Contains some mailbox data.
    /// </summary>
    /// <seealso cref="iMailboxHandle"/>
    public class cMailboxStatus
    {
        internal readonly int MessageCount;
        internal readonly int RecentCount;
        internal readonly uint UIDNext;
        internal readonly int UIDNextUnknownCount; // the number of messages in the mailbox for which we don't know the UID (indicates the inaccuracy of UIDNext)
        internal readonly uint UIDValidity;
        internal readonly int UnseenCount;
        internal readonly int UnseenUnknownCount; // the number of messages in the mailbox for which we don't know the unseen (indicates the inaccuracy of Unseen)
        internal readonly ulong HighestModSeq;

        internal cMailboxStatus(uint pMessages, uint pRecent, uint pUIDNext, uint pUIDValidity, uint pUnseen, ulong pHighestModSeq)
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

        internal cMailboxStatus(int pMessageCount, int pRecentCount, uint pUIDNext, int pUIDNextUnknownCount, uint pUIDValidity, int pUnseenCount, int pUnseenUnknownCount, ulong pHighestModSeq)
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

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxStatus)}({MessageCount},{RecentCount},{UIDNext},{UIDNextUnknownCount},{UIDValidity},{UnseenCount},{UnseenUnknownCount},{HighestModSeq})";

        internal static fMailboxProperties Differences(cMailboxStatus pOld, cMailboxStatus pNew)
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

            return lProperties;
        }
    }
}