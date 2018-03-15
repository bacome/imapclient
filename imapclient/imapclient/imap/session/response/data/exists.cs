using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataExists : cResponseData
            {
                public readonly int Exists;
                public cResponseDataExists(uint pExists) { Exists = (int)pExists; }
                public override string ToString() => $"{nameof(cResponseDataExists)}({Exists})";
            }
        }
    }
}