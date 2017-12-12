using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private class cActiveCommands : List<cCommand>, iTextCodeProcessor
                {
                    public cActiveCommands() { }

                    public void ProcessTextCode(eResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActiveCommands), nameof(ProcessTextCode), pTextContext, pData);
                        foreach (var lCommand in this) lCommand.Hook.ProcessTextCode(pTextContext, pData, lContext);
                    }

                    public void ProcessTextCode(eResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActiveCommands), nameof(ProcessTextCode), pTextContext, pCode, pArguments);
                        foreach (var lCommand in this) lCommand.Hook.ProcessTextCode(pTextContext, pCode, pArguments, lContext);
                    }

                    public override string ToString()
                    {
                        var lBuilder = new cListBuilder(nameof(cActiveCommands));
                        foreach (var lCommand in this) lBuilder.Append(lCommand);
                        return lBuilder.ToString();
                    }
                }
            }
        }
    }
}