using System;

namespace work.bacome.imapclient
{
    public class cMailboxStatus
    {
        public readonly int? Messages;
        public readonly int? Recent;
        public readonly uint? UIDNext;
        public readonly uint? UIDValidity;
        public readonly int? Unseen;

        public cMailboxStatus()
        {
            Messages = null;
            Recent = null;
            UIDNext = null;
            UIDValidity = null;
            Unseen = null;
        }

        public cMailboxStatus(int? pMessages, int? pRecent, uint? pUIDNext, uint? pUIDValidity, int? pUnseen)
        {
            Messages = pMessages;
            Recent = pRecent;
            UIDNext = pUIDNext;
            UIDValidity = pUIDValidity;
            Unseen = pUnseen;
        }

        public static cMailboxStatus Combine(cMailboxStatus pNew, cMailboxStatus pOld)
            => new cMailboxStatus(
                pNew.Messages ?? pOld?.Messages,
                pNew.Recent ?? pOld?.Recent,
                pNew.UIDNext ?? pOld?.UIDNext,
                pNew.UIDValidity ?? pOld?.UIDValidity,
                pNew.Unseen ?? pOld?.Unseen);

        public override string ToString() => $"{nameof(cMailboxStatus)}({Messages},{Recent},{UIDNext},{UIDValidity},{Unseen})";
    }
}