using System;

namespace work.bacome.imapclient
{
    internal class cSelectResult
    {
        public readonly uint UIDValidity;
        public readonly bool UIDNotSticky;

        public cSelectResult(uint pUIDValidity, bool pUIDNotSticky)
        {
            UIDValidity = pUIDValidity;
            UIDNotSticky = pUIDNotSticky;
        }
    }
}
