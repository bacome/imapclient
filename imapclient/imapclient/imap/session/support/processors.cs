﻿using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public enum eProcessDataResult { notprocessed, observed, processed }

            private interface iTextCodeProcessor
            {
                void ProcessTextCode(eIMAPResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext);
                void ProcessTextCode(eIMAPResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext);
            }

            private abstract class cUnsolicitedDataProcessor
            {
                public virtual eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) => eProcessDataResult.notprocessed;
                public virtual eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => eProcessDataResult.notprocessed;
            }
        }
    }
}
