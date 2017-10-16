using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataRecent : cResponseData
            {
                public readonly int Recent;
                public cResponseDataRecent(uint pRecent) { Recent = (int)pRecent; }
                public override string ToString() => $"{nameof(cResponseDataRecent)}({Recent})";
            }
        }
    }
}