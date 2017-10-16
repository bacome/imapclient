using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataFlags : cResponseData
            {
                public readonly cFetchableFlags Flags;
                public cResponseDataFlags(cFetchableFlags pFlags) { Flags = pFlags; }
                public override string ToString() => $"{nameof(cResponseDataFlags)}({Flags})";
            }
        }
    }
}
