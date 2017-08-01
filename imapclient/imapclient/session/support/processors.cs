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
                bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext);
            }

            private class cUnsolicitedDataProcessor
            {
                public virtual eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) => eProcessDataResult.notprocessed;
                public virtual eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => eProcessDataResult.notprocessed;
            }
        }
    }
}
