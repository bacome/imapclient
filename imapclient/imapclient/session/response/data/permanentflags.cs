using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataPermanentFlags : cResponseData
            {
                public readonly cPermanentFlags Flags;
                public cResponseDataPermanentFlags(cPermanentFlags pFlags) { Flags = pFlags; }
                public override string ToString() => $"{nameof(cResponseDataPermanentFlags)}({Flags})";
            }
        }
    }
}