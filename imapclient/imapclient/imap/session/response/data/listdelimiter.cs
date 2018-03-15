using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataListDelimiter : cResponseData
            {
                public readonly char? Delimiter;

                public cResponseDataListDelimiter()
                {
                    Delimiter = null;
                }

                public cResponseDataListDelimiter(char pDelimiter)
                {
                    Delimiter = pDelimiter;
                }

                public override string ToString() => $"{nameof(cResponseDataListDelimiter)}({Delimiter})";
            }
        }
    }
}
