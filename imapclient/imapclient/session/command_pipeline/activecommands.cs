using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.trace;

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

                    public bool ProcessTextCode(eResponseTextType pTextType, cResponseData pData, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActiveCommands), nameof(ProcessTextCode), pTextType, pData);
                        bool lProcessed = false;
                        foreach (var lCommand in this) if (lCommand.Hook.ProcessTextCode(pTextType, pData, lContext)) lProcessed = true;
                        return lProcessed;
                    }

                    public bool ProcessTextCode(eResponseTextType pTextType, cBytesCursor pCursor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActiveCommands), nameof(ProcessTextCode), pTextType);

                        bool lProcessed = false;
                        var lBookmark = pCursor.Position;
                        var lPositionAtEnd = pCursor.Position;

                        foreach (var lCommand in this)
                        {
                            if (lCommand.Hook.ProcessTextCode(pTextType, pCursor, lContext) && !lProcessed)
                            {
                                lProcessed = true;
                                lPositionAtEnd = pCursor.Position;
                            }

                            pCursor.Position = lBookmark;
                        }

                        if (lProcessed) pCursor.Position = lPositionAtEnd;

                        return lProcessed;
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