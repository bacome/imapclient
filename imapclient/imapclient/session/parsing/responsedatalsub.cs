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
                public readonly string EncodedMailboxName;
                public readonly cMailboxName MailboxName;
                public readonly fMailboxFlags MailboxFlags;

                private cResponseDataLSub(string pEncodedMailboxName, cMailboxName pMailboxName, fMailboxFlags pMailboxFlags)
                {
                    EncodedMailboxName = pEncodedMailboxName;
                    MailboxName = pMailboxName;
                    MailboxFlags = pMailboxFlags;
                }

                public override string ToString() => $"{nameof(cResponseDataLSub)}({EncodedMailboxName},{MailboxName},{MailboxFlags})";

                public static bool Process(cBytesCursor pCursor, fEnableableExtensions pEnabledExtensions, out cResponseDataLSub rResponseData, cTrace.cContext pParentContext)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    var lContext = pParentContext.NewMethod(nameof(cResponseDataLSub), nameof(Process), pEnabledExtensions);

                    if (pCursor.GetFlags(out var lFlags) &&
                        pCursor.SkipByte(cASCII.SPACE) &&
                        pCursor.GetMailboxDelimiter(out var lDelimiter) &&
                        pCursor.SkipByte(cASCII.SPACE) &&
                        pCursor.GetAString(out IList<byte> lEncodedMailboxName) &&
                        pCursor.Position.AtEnd &&
                        cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, pEnabledExtensions, out var lMailboxName)) rResponseData = new cResponseDataLSub(cTools.UTF8BytesToString(lEncodedMailboxName), lMailboxName, ZGetMailboxFlags(lFlags));
                    else rResponseData = null;

                    pCursor.ParsedAs = rResponseData;

                    return rResponseData != null;
                }

                private static fMailboxFlags ZGetMailboxFlags(cFlags pFlags)
                {
                    fMailboxFlags lResult = 0;

                    if (pFlags.Has(@"\Noselect")) lResult |= fMailboxFlags.haschildren | fMailboxFlags.notsubscribed | fMailboxFlags.hassubscribedchildren;
                    else lResult |= fMailboxFlags.subscribed;

                    return lResult;
                }
            }
        }
    }
}