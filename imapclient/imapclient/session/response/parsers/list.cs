using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataParserList : iResponseDataParser
            {
                private static readonly cBytes kListSpace = new cBytes("LIST ");

                private bool mUTF8Enabled;

                public cResponseDataParserList(bool pUTF8Enabled)
                {
                    mUTF8Enabled = pUTF8Enabled;
                }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserList), nameof(Process));

                    if (!pCursor.SkipBytes(kListSpace)) { rResponseData = null; return false; }

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxPath) ||
                        !ZProcessExtendedItems(pCursor, out var lExtendedItems) ||
                        !pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed list response");
                        rResponseData = null;
                        return true;
                    }

                    if (lEncodedMailboxPath.Count == 0)
                    {
                        if (lFlags.Count == 0 && lExtendedItems.Count == 0)
                        {
                            if (lDelimiter == null) rResponseData = new cResponseDataListDelimiter();
                            else rResponseData = new cResponseDataListDelimiter((char)lDelimiter.Value);
                        }
                        else
                        {
                            lContext.TraceWarning("likely malformed list delimiter response");
                            rResponseData = null;
                        }
                    }
                    else
                    {
                        if (cMailboxName.TryConstruct(lEncodedMailboxPath, lDelimiter, mUTF8Enabled, out var lMailboxName))
                        {
                            fListFlags lListFlags = 0;

                            if (lFlags.Contains(@"\Noinferiors", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.noinferiors | fListFlags.hasnochildren;
                            if (lFlags.Contains(@"\Noselect", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.noselect;
                            if (lFlags.Contains(@"\Marked", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.marked;
                            if (lFlags.Contains(@"\Unmarked", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.unmarked;

                            if (lFlags.Contains(@"\NonExistent", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.noselect | fListFlags.nonexistent;
                            if (lFlags.Contains(@"\Subscribed", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.subscribed;
                            if (lFlags.Contains(@"\Remote", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.remote;
                            if (lFlags.Contains(@"\HasChildren", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.haschildren;
                            if (lFlags.Contains(@"\HasNoChildren", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.hasnochildren;

                            if (lFlags.Contains(@"\All", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.all;
                            if (lFlags.Contains(@"\Archive", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.archive;
                            if (lFlags.Contains(@"\Drafts", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.drafts;
                            if (lFlags.Contains(@"\Flagged", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.flagged;
                            if (lFlags.Contains(@"\Junk", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.junk;
                            if (lFlags.Contains(@"\Sent", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.sent;
                            if (lFlags.Contains(@"\Trash", StringComparer.InvariantCultureIgnoreCase)) lListFlags |= fListFlags.trash;

                            bool lHasSubscribedChildren = false;

                            foreach (var lItem in lExtendedItems)
                            {
                                if (lItem.Tag.Equals("childinfo", StringComparison.InvariantCultureIgnoreCase) && lItem.Value.Contains("subscribed", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    lHasSubscribedChildren = true;
                                    break;
                                }
                            }

                            rResponseData = new cResponseDataListMailbox(lMailboxName, lListFlags, lHasSubscribedChildren);
                        }
                        else
                        {
                            lContext.TraceWarning("likely malformed list mailbox response");
                            rResponseData = null;
                        }
                    }

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