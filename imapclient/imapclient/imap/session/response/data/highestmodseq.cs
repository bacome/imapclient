using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataHighestModSeq : cResponseData
            {
                public readonly ulong HighestModSeq;
                public cResponseDataHighestModSeq(ulong pHighestModSeq) { HighestModSeq = pHighestModSeq; }
                public override string ToString() => $"{nameof(cResponseDataHighestModSeq)}({HighestModSeq})";
            }
        }
    }
}