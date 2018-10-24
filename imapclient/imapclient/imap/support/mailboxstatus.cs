using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Contains some cached mailbox data. Intended for internal use.
    /// </summary>
    /// <seealso cref="iMailboxHandle"/>
    public class cMailboxStatus
    {
        internal readonly int MessageCount;
        internal readonly int RecentCount;
        internal readonly uint UIDValidity;
        internal readonly uint UIDNextComponent;
        internal readonly cUID UIDNext;
        internal readonly int UIDNextUnknownCount; // the number of messages in the mailbox for which we don't know the UID (indicates the inaccuracy of UIDNext)
        internal readonly int UnseenCount;
        internal readonly int UnseenUnknownCount; // the number of messages in the mailbox for which we don't know the unseen (indicates the inaccuracy of Unseen)
        internal readonly ulong HighestModSeq;

        internal cMailboxStatus(uint pMessages, uint pRecent, uint pUIDValidity, uint pUIDNextComponent, uint pUnseen, ulong pHighestModSeq)
        {
            MessageCount = (int)pMessages;
            RecentCount = (int)pRecent;
            UIDValidity = pUIDValidity;
            UIDNextComponent = pUIDNextComponent;
            if (pUIDValidity == 0 || pUIDNextComponent == 0) UIDNext = null;
            else UIDNext = new cUID(pUIDValidity, pUIDNextComponent);
            UIDNextUnknownCount = 0;
            UnseenCount = (int)pUnseen;
            UnseenUnknownCount = 0;
            HighestModSeq = pHighestModSeq;
        }

        internal cMailboxStatus(int pMessageCount, int pRecentCount, uint pUIDValidity, uint pUIDNextComponent, int pUIDNextUnknownCount, int pUnseenCount, int pUnseenUnknownCount, ulong pHighestModSeq)
        {
            MessageCount = pMessageCount;
            RecentCount = pRecentCount;
            UIDValidity = pUIDValidity;
            UIDNextComponent = pUIDNextComponent;
            if (pUIDValidity == 0 || pUIDNextComponent == 0) UIDNext = null;
            else UIDNext = new cUID(pUIDValidity, pUIDNextComponent);
            UIDNextUnknownCount = pUIDNextUnknownCount;
            UnseenCount = pUnseenCount;
            UnseenUnknownCount = pUnseenUnknownCount;
            HighestModSeq = pHighestModSeq;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxStatus)}({MessageCount},{RecentCount},{UIDValidity},{UIDNextComponent},{UIDNext},{UIDNextUnknownCount},{UnseenCount},{UnseenUnknownCount},{HighestModSeq})";

        internal static fMailboxProperties Differences(cMailboxStatus pOld, cMailboxStatus pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (pOld == null) return 0;

            fMailboxProperties lProperties = 0;

            if (pOld.MessageCount != pNew.MessageCount) lProperties |= fMailboxProperties.messagecount;
            if (pOld.RecentCount != pNew.RecentCount) lProperties |= fMailboxProperties.recentcount;
            if (pOld.UIDValidity != pNew.UIDValidity) lProperties |= fMailboxProperties.uidvalidity;
            if (pOld.UIDNext != pNew.UIDNext) lProperties |= fMailboxProperties.uidnext;
            if (pOld.UIDNextUnknownCount != pNew.UIDNextUnknownCount) lProperties |= fMailboxProperties.uidnextunknowncount;
            if (pOld.UnseenCount != pNew.UnseenCount) lProperties |= fMailboxProperties.unseencount;
            if (pOld.UnseenUnknownCount != pNew.UnseenUnknownCount) lProperties |= fMailboxProperties.unseenunknowncount;
            if (pOld.HighestModSeq != pNew.HighestModSeq) lProperties |= fMailboxProperties.highestmodseq;

            return lProperties;
        }
    }
}