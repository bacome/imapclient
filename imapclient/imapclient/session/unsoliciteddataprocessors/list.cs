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
            private class cListDataProcessor : iUnsolicitedDataProcessor
            {
                private static readonly cBytes kListSpace = new cBytes("LIST ");

                private readonly fEnableableExtensions mEnabledExtensions;
                private readonly dGetCapability mGetCapability;
                private readonly cMailboxCache mMailboxCache;

                public cListDataProcessor(fEnableableExtensions pEnabledExtensions, dGetCapability pGetCapability, cMailboxCache pMailboxCache)
                {
                    mEnabledExtensions = pEnabledExtensions;
                    mGetCapability = pGetCapability;
                    mMailboxCache = pMailboxCache;
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListDataProcessor), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kListSpace)) return eProcessDataResult.notprocessed;

                    if (!pCursor.GetFlags(out var lFlags) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                        !ZProcessExtendedItems(pCursor, out var lExtendedItems) ||
                        !pCursor.Position.AtEnd ||
                        !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mEnabledExtensions, out var lMailboxName))
                    {
                        lContext.TraceWarning("likely malformed list response");
                        return eProcessDataResult.notprocessed;
                    }

                    var lCapability = mGetCapability();

                    fMailboxFlags lMailboxFlags = 0;

                    if (lFlags.Has(@"\Noinferiors")) lMailboxFlags |= fMailboxFlags.noinferiors | fMailboxFlags.hasnochildren;
                    if (lFlags.Has(@"\Noselect")) lMailboxFlags |= fMailboxFlags.noselect;
                    if (lFlags.Has(@"\Marked")) lMailboxFlags |= fMailboxFlags.marked;
                    if (lFlags.Has(@"\Unmarked")) lMailboxFlags |= fMailboxFlags.unmarked;

                    if (lCapability.ListExtended)
                    {
                        if (lFlags.Has(@"\NonExistent")) lMailboxFlags |= fMailboxFlags.noselect | fMailboxFlags.nonexistent;
                        if (lFlags.Has(@"\Subscribed")) lMailboxFlags |= fMailboxFlags.subscribed;
                        if (lFlags.Has(@"\Remote")) lMailboxFlags |= fMailboxFlags.remote;
                    }

                    if (lCapability.Children || lCapability.ListExtended)
                    {
                        if (lFlags.Has(@"\HasChildren")) lMailboxFlags |= fMailboxFlags.haschildren;
                        if (lFlags.Has(@"\HasNoChildren")) lMailboxFlags |= fMailboxFlags.hasnochildren;
                    }

                    if (lCapability.ListExtended && lExtendedItems != null)
                    {
                        foreach (var lItem in lExtendedItems)
                        {
                            if (lItem.Tag.Equals("childinfo", StringComparison.InvariantCultureIgnoreCase))
                            {
                                lMailboxFlags |= fMailboxFlags.haschildren;

                                if (lItem.Value.Contains("subscribed", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    lMailboxFlags |= fMailboxFlags.hassubscribedchildren;
                                    break;
                                }
                            }
                        }
                    }

                    // the special-use capability is to do with support by list-extended, not to do with the return of the attributes
                    if (lFlags.Has(@"\All")) lMailboxFlags |= fMailboxFlags.all;
                    if (lFlags.Has(@"\Archive")) lMailboxFlags |= fMailboxFlags.archive;
                    if (lFlags.Has(@"\Drafts")) lMailboxFlags |= fMailboxFlags.drafts;
                    if (lFlags.Has(@"\Flagged")) lMailboxFlags |= fMailboxFlags.flagged;
                    if (lFlags.Has(@"\Junk")) lMailboxFlags |= fMailboxFlags.junk;
                    if (lFlags.Has(@"\Sent")) lMailboxFlags |= fMailboxFlags.sent;
                    if (lFlags.Has(@"\Trash")) lMailboxFlags |= fMailboxFlags.trash;

                    // store
                    mMailboxCache.SetListFlags(cTools.UTF8BytesToString(lEncodedMailboxName), lMailboxName, lMailboxFlags, lContext);

                    // done
                    return eProcessDataResult.processed;
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

                private class cExtendedItems : List<cExtendedItem>
                {
                    public cExtendedItems() { }

                    public override string ToString()
                    {
                        cListBuilder lBuilder = new cListBuilder(nameof(cExtendedItems));
                        foreach (var lItem in this) lBuilder.Append(lItem);
                        return lBuilder.ToString();
                    }
                }

                private class cExtendedItem
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