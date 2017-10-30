using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataBye : cResponseData
            {
                public readonly cResponseText ResponseText;

                public cResponseDataBye(cResponseText pResponseText)
                {
                    ResponseText = pResponseText;
                }

                public override string ToString() => $"{nameof(cResponseDataBye)}({ResponseText})";
            }
        }
    }
}