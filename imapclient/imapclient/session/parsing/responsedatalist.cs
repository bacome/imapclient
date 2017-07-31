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
            private class cResponseDataList
            {
                public readonly cMailboxName MailboxName;
                public readonly cFlags Flags;
                public readonly cListExtendedItems ExtendedItems;

                private cResponseDataList(cMailboxName pMailboxName, cFlags pFlags, cListExtendedItems pExtendedItems)
                {
                    MailboxName = pMailboxName;
                    Flags = pFlags;
                    ExtendedItems = pExtendedItems;
                }

                public override string ToString() => $"{nameof(cResponseDataESearch)}({MailboxName},{Flags},{ExtendedItems})";

                public static bool Process(cBytesCursor pCursor, bool pUTF8Enabled, out cResponseDataList rResponseData, cTrace.cContext pParentContext)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    var lContext = pParentContext.NewMethod(nameof(cResponseDataList), nameof(Process));

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                        !ZProcessExtendedItems(pCursor, out var lExtendedItems) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, pUTF8Enabled, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed list response");
                        rResponseData = null;
                        pCursor.ParsedAs = null;
                        return false;
                    }

                    rResponseData = new cResponseDataList(lMailboxName, lFlags, lExtendedItems);
                    pCursor.ParsedAs = rResponseData;
                    return true;
                }

                private static bool ZProcessExtendedItems(cBytesCursor pCursor, out cListExtendedItems rItems)
                {
                    rItems = new cListExtendedItems();

                    if (!pCursor.SkipByte(cASCII.SPACE)) return true;
                    if (!pCursor.SkipByte(cASCII.LPAREN)) return false;

                    while (true)
                    {
                        if (!pCursor.GetAString(out string lTag)) break;
                        if (!pCursor.SkipByte(cASCII.SPACE)) return false;
                        if (!pCursor.ProcessExtendedValue(out var lValue)) return false;
                        rItems.Add(new cListExtendedItem(lTag, lValue));
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    if (!pCursor.SkipByte(cASCII.RPAREN)) return false;

                    return true;
                }
            }
        }
    }
}