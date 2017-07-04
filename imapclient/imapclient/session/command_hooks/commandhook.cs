using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHook : iTextCodeProcessor
            {
                public cCommandHook() { }

                // called before process routines BUT will not be called at all if the command times out before being submitted
                //  this is here specially for select
                //
                public virtual void CommandStarted(cTrace.cContext pParentContext) { }

                // process responses while the command is running
                public virtual eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) { return eProcessDataResult.notprocessed; }
                public virtual bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext) { return false; }

                // called last, but may be called at any time
                //  if the command was not submitted, then the result will be null and the exception may or may not be null (depending), and the call is not deterministic (it may be made some time after the decision not to submit)
                //  if the command was submitted then this will be called before the pipeline does any further processing of commands or responses => it is a safe place to resolve MSNs
                //   (note though that the exception may be set)
                //  
                public virtual void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext) { }
            }
        }
    }
}
