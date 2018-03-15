using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataAccess : cResponseData
            {
                public readonly bool ReadOnly;
                public cResponseDataAccess(bool pReadOnly) { ReadOnly = pReadOnly; }
                public override string ToString() => $"{nameof(cResponseDataAccess)}({ReadOnly})";
            }
        }
    }
}