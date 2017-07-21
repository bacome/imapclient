using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    private partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache
            {
                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");

                private static readonly cBytes kListSpace = new cBytes("LIST ");

                private static readonly cBytes kStatusSpace = new cBytes("STATUS ");
                private static readonly cBytes kMessagesSpace = new cBytes("MESSAGES ");
                private static readonly cBytes kRecentSpace = new cBytes("RECENT ");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");

                private enum eProcessStatusAttributeResult { notprocessed, processed, error }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ProcessData));

                    if (mSelectedMailbox != null)
                    {
                        if (pCursor.SkipBytes(kFlagsSpace))
                        {
                            if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd)
                            {
                                lContext.TraceVerbose("got flags: {0}", lFlags);

                                var lItem = mSelectedMailbox.Handle as cItem;
                                lItem.SetMessageFlags(new cMessageFlags(lFlags), lContext);
                                return eProcessDataResult.processed;
                            }

                            lContext.TraceWarning("likely malformed flags response");
                            return eProcessDataResult.notprocessed;
                        }

                        var lBookmark = pCursor.Position;
                        var lResult = mSelectedMailbox.ProcessData(pCursor, lContext);
                        if (lResult != eProcessDataResult.notprocessed) return lResult;
                        pCursor.Position = lBookmark;
                    }

                    if (pCursor.SkipBytes(kListSpace))
                    {
                        if (!pCursor.GetFlags(out var lFlags) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                            !ZProcessListExtendedItems(pCursor, out var lExtendedItems) ||
                            !pCursor.Position.AtEnd ||
                            !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mUTF8Enabled, out var lMailboxName))
                        {
                            lContext.TraceWarning("likely malformed list response");
                            return eProcessDataResult.notprocessed;
                        }

                        var lItem = ZItem(lMailboxName);
                        lItem.SetMailboxFlags(mSequence++, ZProcessListMailboxFlags(lFlags, lExtendedItems), lContext);
                    }

                    if (pCursor.SkipBytes(kStatusSpace))
                    {
                        if (!pCursor.GetAString(out string lEncodedMailboxName) ||
                            !pCursor.SkipBytes(cBytesCursor.SpaceLParen) ||
                            !ZProcessStatusAttributes(pCursor, out var lStatus, lContext) ||
                            !pCursor.SkipByte(cASCII.RPAREN) ||
                            !pCursor.Position.AtEnd)
                        {
                            lContext.TraceWarning("likely malformed status response");
                            return eProcessDataResult.notprocessed;
                        }

                        var lItem = ZItem(lEncodedMailboxName);

                        ;?; // sequence interlocked
                        Sequence = Interlocked.Increment(ref mLastSequence);


                        lItem.UpdateStatus(lStatus);

                        if (!ReferenceEquals(lItem, mSelectedMailbox?.Handle))
                        {
                            var lProperties = lItem.UpdateMailboxStatus();
                            if (lProperties != 0) mEventSynchroniser.FireMailboxPropertiesChanged(lItem, lProperties, lContext);
                        }

                        return eProcessDataResult.processed;
                    }

                    ;?;








                }


                private bool ZProcessListExtendedItems(cBytesCursor pCursor, out cExtendedItems rItems)
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

                private cMailboxFlags ZProcessListMailboxFlags(cFlags pFlags, cExtendedItems pExtendedItems)
                {
                    fMailboxFlags lFlags = 0;

                    if (pFlags.Has(@"\Noinferiors")) lFlags |= fMailboxFlags.noinferiors | fMailboxFlags.hasnochildren;
                    if (pFlags.Has(@"\Noselect")) lFlags |= fMailboxFlags.noselect;
                    if (pFlags.Has(@"\Marked")) lFlags |= fMailboxFlags.marked;
                    if (pFlags.Has(@"\Unmarked")) lFlags |= fMailboxFlags.unmarked;

                    if (mCapability.ListExtended)
                    {
                        if (pFlags.Has(@"\NonExistent")) lFlags |= fMailboxFlags.noselect | fMailboxFlags.nonexistent;
                        if (pFlags.Has(@"\Subscribed")) lFlags |= fMailboxFlags.subscribed;
                        if (pFlags.Has(@"\Remote")) lFlags |= fMailboxFlags.remote;
                    }

                    if (mCapability.Children || mCapability.ListExtended)
                    {
                        if (pFlags.Has(@"\HasChildren")) lFlags |= fMailboxFlags.haschildren;
                        if (pFlags.Has(@"\HasNoChildren")) lFlags |= fMailboxFlags.hasnochildren;
                    }

                    if (mCapability.ListExtended && pExtendedItems != null)
                    {
                        foreach (var lItem in pExtendedItems)
                        {
                            if (lItem.Tag.Equals("childinfo", StringComparison.InvariantCultureIgnoreCase))
                            {
                                lFlags |= fMailboxFlags.haschildren;

                                if (lItem.Value.Contains("subscribed", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    lFlags |= fMailboxFlags.hassubscribedchildren;
                                    break;
                                }
                            }
                        }
                    }

                    // the special-use capability is to do with support by list-extended, not to do with the return of the attributes
                    if (pFlags.Has(@"\All")) lFlags |= fMailboxFlags.all;
                    if (pFlags.Has(@"\Archive")) lFlags |= fMailboxFlags.archive;
                    if (pFlags.Has(@"\Drafts")) lFlags |= fMailboxFlags.drafts;
                    if (pFlags.Has(@"\Flagged")) lFlags |= fMailboxFlags.flagged;
                    if (pFlags.Has(@"\Junk")) lFlags |= fMailboxFlags.junk;
                    if (pFlags.Has(@"\Sent")) lFlags |= fMailboxFlags.sent;
                    if (pFlags.Has(@"\Trash")) lFlags |= fMailboxFlags.trash;

                    return new cMailboxFlags(lFlags);
                }

                private bool ZProcessStatusAttributes(cBytesCursor pCursor, out cStatus rStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessStatusAttributes));

                    uint? lMessages = 0;
                    uint? lRecent = 0;
                    uint? lUIDNext = 0;
                    uint? lUIDValidity = 0;
                    uint? lUnseen = 0;
                    ulong? lHighestModSeq = 0;

                    while (true)
                    {
                        eProcessStatusAttributeResult lResult;

                        lResult = ZProcessStatusAttribute(pCursor, kMessagesSpace, ref lMessages, lContext);

                        if (lResult == eProcessStatusAttributeResult.notprocessed)
                        {
                            lResult = ZProcessStatusAttribute(pCursor, kRecentSpace, ref lRecent, lContext);

                            if (lResult == eProcessStatusAttributeResult.notprocessed)
                            {
                                lResult = ZProcessStatusAttribute(pCursor, kUIDNextSpace, ref lUIDNext, lContext);

                                if (lResult == eProcessStatusAttributeResult.notprocessed)
                                {
                                    lResult = ZProcessStatusAttribute(pCursor, kUIDValiditySpace, ref lUIDValidity, lContext);

                                    if (lResult == eProcessStatusAttributeResult.notprocessed)
                                    {
                                        lResult = ZProcessStatusAttribute(pCursor, kUnseenSpace, ref lUnseen, lContext);

                                        if (lResult == eProcessStatusAttributeResult.notprocessed) lResult = ZProcessStatusAttribute(pCursor, kHighestModSeqSpace, ref lHighestModSeq, lContext);
                                    }
                                }
                            }
                        }

                        if (lResult != eProcessStatusAttributeResult.processed)
                        {
                            rStatus = null;
                            return false;
                        }

                        if (!pCursor.SkipByte(cASCII.SPACE))
                        {
                            rStatus = new cStatus(lMessages, lRecent, lUIDNext, lUIDValidity, lUnseen, lHighestModSeq);
                            return true;
                        }
                    }
                }

                private static eProcessStatusAttributeResult ZProcessStatusAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref uint? rNumber, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessStatusAttribute), pAttributeSpace);

                    if (!pCursor.SkipBytes(pAttributeSpace)) return eProcessStatusAttributeResult.notprocessed;

                    if (pCursor.GetNumber(out _, out var lNumber))
                    {
                        lContext.TraceVerbose("got {0}", lNumber);
                        rNumber = lNumber;
                        return eProcessStatusAttributeResult.processed;
                    }

                    lContext.TraceWarning("likely malformed status-att-list-item: no number?");
                    return eProcessStatusAttributeResult.error;
                }

                private static eProcessStatusAttributeResult ZProcessStatusAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref ulong? rNumber, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessStatusAttribute));

                    if (!pCursor.SkipBytes(pAttributeSpace)) return eProcessStatusAttributeResult.notprocessed;

                    if (pCursor.GetNumber(out var lNumber))
                    {
                        lContext.TraceVerbose("got {0}", lNumber);
                        rNumber = lNumber;
                        return eProcessStatusAttributeResult.processed;
                    }

                    lContext.TraceWarning("likely malformed status-att-list-item: no number?");
                    return eProcessStatusAttributeResult.error;
                }

            }
        }
    }
}