using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public enum eProcessDataResult { notprocessed, observed, processed }

            private interface iTextCodeProcessor
            {
                void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext);
                bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext);
            }

            private interface iUnsolicitedDataProcessor
            {
                eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext);
                eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext);
            }
        }
    }
}
