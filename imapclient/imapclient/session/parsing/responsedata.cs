using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cResponseData { }

            private abstract class cResponseDataParser
            {
                public abstract bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext);
            }
        }
    }
}