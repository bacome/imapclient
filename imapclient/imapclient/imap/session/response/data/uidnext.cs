using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataUIDNext : cResponseData
            {
                public readonly uint UIDNext;
                public cResponseDataUIDNext(uint pUIDNext) { UIDNext = pUIDNext; }
                public override string ToString() => $"{nameof(cResponseDataUIDNext)}({UIDNext})";
            }
        }
    }
}