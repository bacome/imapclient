using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataHighestModSeq : cResponseData
            {
                public readonly uint HighestModSeq;
                public cResponseDataHighestModSeq(uint pHighestModSeq) { HighestModSeq = pHighestModSeq; }
                public override string ToString() => $"{nameof(cResponseDataHighestModSeq)}({HighestModSeq})";
            }
        }
    }
}