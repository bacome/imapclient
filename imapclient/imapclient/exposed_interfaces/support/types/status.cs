using System;

namespace work.bacome.imapclient.support
{
    public class cStatus
    {
        public readonly int Sequence;
        public readonly uint? Messages;
        public readonly uint? Recent;
        public readonly uint? UIDNext;
        public readonly uint? UIDValidity;
        public readonly uint? Unseen;
        public readonly ulong? HighestModSeq;

        public cStatus(int pSequence, uint? pMessages, uint? pRecent, uint? pUIDNext, uint? pUIDValidity, uint? pUnseen, ulong? pHighestModSeq)
        {
            Sequence = pSequence;
            Messages = pMessages;
            Recent = pRecent;
            UIDNext = pUIDNext;
            UIDValidity = pUIDValidity;
            Unseen = pUnseen;
            HighestModSeq = pHighestModSeq;
        }

        public static cStatus Combine(cStatus pOld, cStatus pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));
            if (pOld == null) return pNew;

            return
                new cStatus
                    (
                        pNew.Sequence,
                        pNew.Messages ?? pOld.Messages,
                        pNew.Recent ?? pOld.Recent,
                        pNew.UIDNext ?? pOld.UIDNext,
                        pNew.UIDValidity ?? pOld.UIDValidity,
                        pNew.Unseen ?? pOld.Unseen,
                        pNew.HighestModSeq ?? pOld.HighestModSeq
                    );
        }

        public override string ToString() => $"{nameof(cStatus)}({Sequence},{Messages},{Recent},{UIDNext},{UIDValidity},{Unseen},{HighestModSeq})";
    }
}