using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataUIDValidity : cResponseData
            {
                public readonly uint UIDValidity;
                public cResponseDataUIDValidity(uint pUIDValidity) { UIDValidity = pUIDValidity; }
                public override string ToString() => $"{nameof(cResponseDataUIDValidity)}({UIDValidity})";
            }
        }
    }
}