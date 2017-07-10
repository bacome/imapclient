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
                public readonly cBytesCursor.cFlags Flags;
                public readonly string EncodedMailboxName;
                public readonly cMailboxName MailboxName;
                public readonly cExtendedItems ExtendedItems;

                private cResponseDataList(cBytesCursor.cFlags pFlags, string pEncodedMailboxName, cMailboxName pMailboxName, cExtendedItems pExtendedItems)
                {
                    Flags = pFlags;
                    EncodedMailboxName = pEncodedMailboxName;
                    MailboxName = pMailboxName;
                    ExtendedItems = pExtendedItems;
                }

                public override string ToString() => $"{nameof(cResponseDataList)}({Flags},{EncodedMailboxName},{MailboxName},{ExtendedItems})";

                public static bool Process(cBytesCursor pCursor, fEnableableExtensions pEnabledExtensions, out cResponseDataList rResponse, cTrace.cContext pParentContext)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    var lContext = pParentContext.NewMethod(nameof(cResponseDataList), nameof(Process), pEnabledExtensions);

                    if (pCursor.GetFlags(out var lFlags) &&
                        pCursor.SkipByte(cASCII.SPACE) &&
                        pCursor.GetMailboxDelimiter(out var lDelimiter) &&
                        pCursor.SkipByte(cASCII.SPACE) &&
                        pCursor.GetAString(out IList<byte> lEncodedMailboxName) &&
                        ZProcessExtendedItems(pCursor, out var lExtendedItems) &&
                        pCursor.Position.AtEnd &&
                        cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, pEnabledExtensions, out var lMailboxName)) rResponse = new cResponseDataList(lFlags, cTools.UTF8BytesToString(lEncodedMailboxName), lMailboxName, lExtendedItems);
                    else rResponse = null;

                    pCursor.ParsedAs = rResponse;

                    return rResponse != null;
                }

                private static bool ZProcessExtendedItems(cBytesCursor pCursor, out cExtendedItems rItems)
                {
                    rItems = new cExtendedItems();

                    if (!pCursor.SkipByte(cASCII.SPACE)) return true;
                    if (!pCursor.SkipByte(cASCII.LPAREN)) return false;

                    while (true)
                    {
                        if (!pCursor.GetAString(out string lTag)) break;
                        if (!pCursor.SkipByte(cASCII.SPACE)) return false;
                        if (!pCursor.ProcessExtendedValue(out var lValue)) return false;
                        rItems.Add(new cExtendedItem(lTag, lValue));
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    if (!pCursor.SkipByte(cASCII.RPAREN)) return false;

                    return true;
                }

                /*
                private static fMailboxFlags ZGetMailboxFlags(cCapability pCapability, bool pRemote, cBytesCursor.cFlags pFlags, cExtendedItems pListExtendedItems)
                {
                    fMailboxFlags lResult = 0;

                    if (pFlags.Has(@"\Noinferiors")) lResult |= fMailboxFlags.noinferiors | fMailboxFlags.hasnochildren;

                    if (pCapability.Children)
                    {
                        if (pFlags.Has(@"\HasChildren")) lResult |= fMailboxFlags.haschildren;
                        if (pFlags.Has(@"\HasNoChildren")) lResult |= fMailboxFlags.hasnochildren;
                    }

                    if (pCapability.ListExtended)
                    {
                        if (pFlags.Has(@"\NonExistent")) lResult |= fMailboxFlags.noselect;
                        if (pFlags.Has(@"\Subscribed")) lResult |= fMailboxFlags.subscribed;
                        if (!pFlags.Has(@"\Remote")) lResult |= fMailboxFlags.local;

                        ;?; // this is meant to be for hassubscribed children ...
                        if (pListExtendedItems != null)
                            if (pListExtendedItems.Any(lExtendedItem => lExtendedItem.Tag.Equals("childinfo", StringComparison.InvariantCultureIgnoreCase) && lExtendedItem.Value.Contains("subscribed", StringComparison.InvariantCultureIgnoreCase)))
                                lResult |= fMailboxFlags.haschildren;
                    }
                    else
                    {
                        if ()
                    }

                    if (pFlags.Has(@"\Noselect")) lResult |= fMailboxFlags.noselect;

                    if (pFlags.Has(@"\Marked")) lResult |= fMailboxFlags.marked;
                    if (pFlags.Has(@"\Unmarked")) lResult |= fMailboxFlags.unmarked;

                    if (pCapability.SpecialUse)
                    {
                        if (pFlags.Has(@"\All")) lResult |= fMailboxFlags.allmessages;
                        if (pFlags.Has(@"\Archive")) lResult |= fMailboxFlags.archive;
                        if (pFlags.Has(@"\Drafts")) lResult |= fMailboxFlags.drafts;
                        if (pFlags.Has(@"\Flagged")) lResult |= fMailboxFlags.flagged;
                        if (pFlags.Has(@"\Junk")) lResult |= fMailboxFlags.junk;
                        if (pFlags.Has(@"\Sent")) lResult |= fMailboxFlags.sent;
                        if (pFlags.Has(@"\Trash")) lResult |= fMailboxFlags.trash;
                    }

                    return lResult;
                }
                */

                public class cExtendedItems : List<cExtendedItem>
                {
                    public cExtendedItems() { }

                    public override string ToString()
                    {
                        cListBuilder lBuilder = new cListBuilder(nameof(cExtendedItems));
                        foreach (var lItem in this) lBuilder.Append(lItem);
                        return lBuilder.ToString();
                    }
                }

                public class cExtendedItem
                {
                    public readonly string Tag;
                    public readonly cExtendedValue Value; // can be null if the value is ()

                    public cExtendedItem(string pTag, cExtendedValue pValue)
                    {
                        Tag = pTag;
                        Value = pValue;
                    }

                    public override string ToString() => $"{nameof(cExtendedItem)}({Tag},{Value})";
                }
            }
        }
    }
}