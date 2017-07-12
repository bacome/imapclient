using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cStatus
            {
                public readonly int? Messages;
                public readonly int? Recent;
                public readonly uint? UIDNext;
                public readonly uint? UIDValidity;
                public readonly int? Unseen;
                public readonly ulong? HighestModSeq;

                public cStatus(int? pMessages, int? pRecent, uint? pUIDNext, uint? pUIDValidity, int? pUnseen, ulong? pHighestModSeq)
                {
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
                                pNew.Messages ?? pOld.Messages,
                                pNew.Recent ?? pOld.Recent,
                                pNew.UIDNext ?? pOld.UIDNext,
                                pNew.UIDValidity ?? pOld.UIDValidity,
                                pNew.Unseen ?? pOld.Unseen,
                                pNew.HighestModSeq ?? pOld.HighestModSeq
                            );
                }

                public override string ToString() => $"{nameof(cStatus)}({Messages},{Recent},{UIDNext},{UIDValidity},{Unseen},{HighestModSeq})";
            }
        }
    }
}