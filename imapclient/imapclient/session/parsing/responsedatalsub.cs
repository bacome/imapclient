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
            private class cResponseDataLSub : cResponseData
            {
                public readonly cMailboxName MailboxName;
                public readonly bool Subscribed;

                public cResponseDataLSub(cMailboxName pMailboxName, bool pSubscribed)
                {
                    MailboxName = pMailboxName;
                    Subscribed = pSubscribed;
                }

                public override string ToString() => $"{nameof(cResponseDataLSub)}({MailboxName},{Subscribed})";
            }

            private class cResponseDataParserLSub : iResponseDataParser
            {
                private static readonly cBytes kLSubSpace = new cBytes("LSUB ");

                private bool mUTF8Enabled;

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
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mUTF8Enabled, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed lsub response");
                        rResponseData = null;
                        return true;
                    }

                    rResponseData = new cResponseDataLSub(lMailboxName, !lFlags.Has(@"\Noselect"));
                    return true;
                }
            }
        }
    }
}