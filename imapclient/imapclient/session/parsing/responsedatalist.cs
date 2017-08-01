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
            private class cResponseDataList : cResponseData
            {
                public readonly cMailboxName MailboxName;
                public readonly fListFlags Flags;
                public readonly cListExtendedItems ExtendedItems;

                public cResponseDataList(cMailboxName pMailboxName, fListFlags pFlags, cListExtendedItems pExtendedItems)
                {
                    MailboxName = pMailboxName;
                    Flags = pFlags;
                    ExtendedItems = pExtendedItems;
                }

                public override string ToString() => $"{nameof(cResponseDataList)}({MailboxName},{Flags},{ExtendedItems})";
            }

            private class cResponseDataParserList : cResponseDataParser
            {
                private static readonly cBytes kListSpace = new cBytes("LIST ");

                private bool mUTF8Enabled;

                public cResponseDataParserList(bool pUTF8Enabled)
                {
                    mUTF8Enabled = pUTF8Enabled;
                }

                public override bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserList), nameof(Process));

                    if (!pCursor.SkipBytes(kListSpace)) { rResponseData = null; return false; }

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                        !ZProcessExtendedItems(pCursor, out var lExtendedItems) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mUTF8Enabled, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed list response");
                        rResponseData = null;
                        return true;
                    }

                    fListFlags lListFlags = 0;

                    if (lFlags.Has(@"\Noinferiors")) lListFlags |= fListFlags.noinferiors | fListFlags.hasnochildren;
                    if (lFlags.Has(@"\Noselect")) lListFlags |= fListFlags.noselect;
                    if (lFlags.Has(@"\Marked")) lListFlags |= fListFlags.marked;
                    if (lFlags.Has(@"\Unmarked")) lListFlags |= fListFlags.unmarked;

                    if (lFlags.Has(@"\NonExistent")) lListFlags |= fListFlags.noselect | fListFlags.nonexistent;
                    if (lFlags.Has(@"\Subscribed")) lListFlags |= fListFlags.noselect | fListFlags.subscribed;
                    if (lFlags.Has(@"\Remote")) lListFlags |= fListFlags.remote;
                    if (lFlags.Has(@"\HasChildren")) lListFlags |= fListFlags.haschildren;
                    if (lFlags.Has(@"\HasNoChildren")) lListFlags |= fListFlags.hasnochildren;

                    if (lFlags.Has(@"\All")) lListFlags |= fListFlags.all;
                    if (lFlags.Has(@"\Archive")) lListFlags |= fListFlags.archive;
                    if (lFlags.Has(@"\Drafts")) lListFlags |= fListFlags.drafts;
                    if (lFlags.Has(@"\Flagged")) lListFlags |= fListFlags.flagged;
                    if (lFlags.Has(@"\Junk")) lListFlags |= fListFlags.junk;
                    if (lFlags.Has(@"\Sent")) lListFlags |= fListFlags.sent;
                    if (lFlags.Has(@"\Trash")) lListFlags |= fListFlags.trash;

                    rResponseData = new cResponseDataList(lMailboxName, lListFlags, lExtendedItems);
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