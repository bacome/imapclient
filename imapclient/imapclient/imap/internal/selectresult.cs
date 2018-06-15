using System;

namespace work.bacome.imapclient
{
    internal class cSelectResult
    {
        public readonly uint UIDValidity;
        public readonly ulong HighestModSeq;
        public readonly bool UIDNotSticky;

        public cSelectResult(uint pUIDValidity, ulong pHighestModSeq, bool pUIDNotSticky)
        {
            UIDValidity = pUIDValidity;
            HighestModSeq = pHighestModSeq;
            UIDNotSticky = pUIDNotSticky;
        }
    }
}
