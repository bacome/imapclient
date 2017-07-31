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
            private class cResponseDataLSub
            {
                public readonly cMailboxName MailboxName;
                public readonly cFlags Flags;

                private cResponseDataLSub(cMailboxName pMailboxName, cFlags pFlags)
                {
                    MailboxName = pMailboxName;
                    Flags = pFlags;
                }

                public override string ToString() => $"{nameof(cResponseDataESearch)}({MailboxName},{Flags})";

                public static bool Process(cBytesCursor pCursor, bool pUTF8Enabled, out cResponseDataLSub rResponseData, cTrace.cContext pParentContext)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    var lContext = pParentContext.NewMethod(nameof(cResponseDataLSub), nameof(Process));

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, pUTF8Enabled, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed lsub response");
                        rResponseData = null;
                        pCursor.ParsedAs = null;
                        return false;
                    }

                    rResponseData = new cResponseDataLSub(lMailboxName, lFlags);
                    pCursor.ParsedAs = rResponseData;
                    return true;
                }
            }
        }
    }
}