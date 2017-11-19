using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cResponseData { }

            private interface iResponseDataParser
            {
                bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext);
            }

            private interface iResponseTextCodeParser
            {
                bool Process(cByteList pCode, cByteList pArguments, out cResponseData rResponseData, cTrace.cContext pParentContext);
            }
        }
    }
}