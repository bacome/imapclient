using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHook : iTextCodeProcessor
            {
                public static readonly cCommandHook DoNothing = new cCommandHook();

                protected cCommandHook() { }

                // called just before the command is submitted to the server
                public virtual void CommandStarted(cTrace.cContext pParentContext) { }

                // process responses while the command is running
                public virtual eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) { return eProcessDataResult.notprocessed; }
                public virtual eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) { return eProcessDataResult.notprocessed; }
                public virtual void ProcessTextCode(eResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext) { }
                public virtual void ProcessTextCode(eResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext) { }
                
                // called on getting command completion from the server => it is a safe place to resolve MSNs
                public virtual void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext) { }
            }
        }
    }
}
