using System;

namespace work.bacome.imapclient
{
    internal class cStatus
    {
        public readonly int Sequence;
        public readonly uint? Messages;
        public readonly uint? Recent;
        public readonly uint? UIDValidity;
        public readonly uint? UIDNextComponent;
        public readonly uint? Unseen;
        public readonly ulong? HighestModSeq;

        public cStatus(int pSequence, uint? pMessages, uint? pRecent, uint? pUIDValidity, uint? pUIDNextComponent, uint? pUnseen, ulong? pHighestModSeq)
        {
            Sequence = pSequence;
            Messages = pMessages;
            Recent = pRecent;
            UIDValidity = pUIDValidity;
            UIDNextComponent = pUIDNextComponent;
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
                        pNew.UIDValidity ?? pOld.UIDValidity,
                        pNew.UIDNextComponent ?? pOld.UIDNextComponent,
                        pNew.Unseen ?? pOld.Unseen,
                        pNew.HighestModSeq ?? pOld.HighestModSeq
                    );
        }

        public override string ToString() => $"{nameof(cStatus)}({Sequence},{Messages},{Recent},{UIDValidity},{UIDNextComponent},{Unseen},{HighestModSeq})";
    }
}