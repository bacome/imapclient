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
            private class cResponseDataListDelimiter : cResponseData
            {
                public readonly char? Delimiter;

                public cResponseDataListDelimiter()
                {
                    Delimiter = null;
                }

                public cResponseDataListDelimiter(char pDelimiter)
                {
                    Delimiter = pDelimiter;
                }

                public override string ToString() => $"{nameof(cResponseDataListDelimiter)}({Delimiter})";
            }

            private class cResponseDataListMailbox : cResponseData
            {
                public readonly cMailboxName MailboxName;
                public readonly fListFlags Flags;
                public readonly bool HasSubscribedChildren;

                public cResponseDataListMailbox(cMailboxName pMailboxName, fListFlags pFlags, bool pHasSubscribedChildren)
                {
                    MailboxName = pMailboxName;
                    Flags = pFlags;
                    HasSubscribedChildren = pHasSubscribedChildren;
                }

                public override string ToString() => $"{nameof(cResponseDataListMailbox)}({MailboxName},{Flags},{HasSubscribedChildren})";
            }

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

                            if (lFlags.Has(@"\Noinferiors")) lListFlags |= fListFlags.noinferiors | fListFlags.hasnochildren;
                            if (lFlags.Has(@"\Noselect")) lListFlags |= fListFlags.noselect;
                            if (lFlags.Has(@"\Marked")) lListFlags |= fListFlags.marked;
                            if (lFlags.Has(@"\Unmarked")) lListFlags |= fListFlags.unmarked;

                            if (lFlags.Has(@"\NonExistent")) lListFlags |= fListFlags.noselect | fListFlags.nonexistent;
                            if (lFlags.Has(@"\Subscribed")) lListFlags |= fListFlags.subscribed;
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