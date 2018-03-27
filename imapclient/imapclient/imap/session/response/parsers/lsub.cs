using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataParserLSub : iResponseDataParser
            {
                private static readonly cBytes kLSubSpace = new cBytes("LSUB ");

                private readonly bool mUTF8Enabled;

                public cResponseDataParserLSub(bool pUTF8Enabled)
                {
                    mUTF8Enabled = pUTF8Enabled;
                }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserLSub), nameof(Process));

                    if (!pCursor.SkipBytes(kLSubSpace)) { rResponseData = null; return false; }

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxPath) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxPath, lDelimiter, mUTF8Enabled, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed lsub response");
                        rResponseData = null;
                        return true;
                    }

                    rResponseData = new cResponseDataLSub(lMailboxName, !lFlags.Contains(@"\Noselect", StringComparer.InvariantCultureIgnoreCase));
                    return true;
                }
            }
        }
    }
}