using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataBye : cResponseData
            {
                public readonly cIMAPResponseText ResponseText;

                public cResponseDataBye(cIMAPResponseText pResponseText)
                {
                    ResponseText = pResponseText;
                }

                public override string ToString() => $"{nameof(cResponseDataBye)}({ResponseText})";
            }
        }
    }
}