using System;
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
                bool ProcessTextCode(eResponseTextType pTextType, cResponseData pData, cTrace.cContext pParentContext);
                bool ProcessTextCode(eResponseTextType pTextType, cBytesCursor pCursor, cTrace.cContext pParentContext);
            }

            private abstract class cUnsolicitedDataProcessor
            {
                public virtual eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) => eProcessDataResult.notprocessed;
                public virtual eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => eProcessDataResult.notprocessed;
            }
        }
    }
}
