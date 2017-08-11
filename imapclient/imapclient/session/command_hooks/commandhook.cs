﻿using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cCommandHook : iTextCodeProcessor
            {
                public cCommandHook() { }

                // called just before the command is submitted to the server
                public virtual void CommandStarted(cTrace.cContext pParentContext) { }

                // process responses while the command is running
                public virtual eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) { return eProcessDataResult.notprocessed; }
                public virtual eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) { return eProcessDataResult.notprocessed; }
                public virtual void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext) { }
                public virtual bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext) { return false; }

                // called on getting command completion from the server => it is a safe place to resolve MSNs
                public virtual void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext) { }
            }
        }
    }
}
