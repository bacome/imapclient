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
                private static readonly cBytes kListSpace = new cBytes("LIST ");
                private static readonly cBytes kLSubSpace = new cBytes("LSUB ");

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
                            !ZProcessDataListExtendedItems(pCursor, out var lExtendedItems) ||
                            !pCursor.Position.AtEnd ||
                            !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mCommandPartFactory.UTF8Enabled, out var lMailboxName))
                        {
                            lContext.TraceWarning("likely malformed list response");
                            return eProcessDataResult.notprocessed;
                        }

                        ZProcessDataListMailboxFlags(lFlags, lExtendedItems, out var lListFlags, out var lLSubFlags);

                        var lItem = ZItem(lMailboxName);
                        lItem.SetFlags(lListFlags, lLSubFlags, lContext);

                        return eProcessDataResult.processed;
                    }

                    if (pCursor.SkipBytes(kLSubSpace))
                    {
                        if (!pCursor.GetFlags(out var lFlags) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetAString(out IList<byte> lEncodedMailboxName) ||
                            !pCursor.Position.AtEnd ||
                            !cMailboxName.TryConstruct(lEncodedMailboxName, lDelimiter, mCommandPartFactory.UTF8Enabled, out var lMailboxName))
                        {
                            lContext.TraceWarning("likely malformed lsub response");
                            return eProcessDataResult.notprocessed;
                        }

                        ZProcessDataLSubMailboxFlags(lFlags, out var lLSubFlags);

                        var lItem = ZItem(lMailboxName);
                        lItem.SetFlags(lLSubFlags, lContext);

                        return eProcessDataResult.processed;
                    }

                    if (pCursor.SkipBytes(kStatusSpace))
                    {
                        if (!pCursor.GetAString(out string lEncodedMailboxName) ||
                            !pCursor.SkipBytes(cBytesCursor.SpaceLParen) ||
                            !ZProcessDataStatusAttributes(pCursor, out var lStatus, lContext) ||
                            !pCursor.SkipByte(cASCII.RPAREN) ||
                            !pCursor.Position.AtEnd)
                        {
                            lContext.TraceWarning("likely malformed status response");
                            return eProcessDataResult.notprocessed;
                        }

                        var lItem = ZItem(lEncodedMailboxName);
                        lItem.UpdateStatus(lStatus, lContext);
                        if (!ReferenceEquals(mSelectedMailbox?.mMailboxCacheItem, lItem)) lItem.UpdateMailboxStatus(lContext);

                        return eProcessDataResult.processed;
                    }

                    ;?;







                    return eProcessDataResult.notprocessed;
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ProcessTextCode));

                    if (mSelectedMailbox != null) return mSelectedMailbox.ProcessTextCode(pCursor, lContext);
                    return false;

                    {

                        ;?;
                        var lBookmark = pCursor.Position;
                        var lResult = mSelectedMailbox.ProcessData(pCursor, lContext);
                        if (lResult != eProcessDataResult.notprocessed) return lResult;
                        pCursor.Position = lBookmark;
                    }

                    return false;
                }

                private bool ZProcessDataListExtendedItems(cBytesCursor pCursor, out cExtendedItems rItems)
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

                private void ZProcessDataListMailboxFlags(cFlags pFlags, cExtendedItems pExtendedItems, out cListFlags rListFlags, out cLSubFlags rLSubFlags)
                {
                    fListFlags lListFlags = 0;
                    fLSubFlags lLSubFlags = 0;

                    if (pFlags.Has(@"\Noinferiors")) lListFlags |= fListFlags.noinferiors | fListFlags.hasnochildren;
                    if (pFlags.Has(@"\Noselect")) lListFlags |= fListFlags.noselect;
                    if (pFlags.Has(@"\Marked")) lListFlags |= fListFlags.marked;
                    if (pFlags.Has(@"\Unmarked")) lListFlags |= fListFlags.unmarked;

                    if (mCapability.ListExtended)
                    {
                        if (pFlags.Has(@"\NonExistent")) lListFlags |= fListFlags.noselect | fListFlags.nonexistent;
                        if (pFlags.Has(@"\Subscribed")) lLSubFlags |= fLSubFlags.subscribed;
                        if (pFlags.Has(@"\Remote")) lListFlags |= fListFlags.remote;
                    }

                    if (mCapability.Children || mCapability.ListExtended)
                    {
                        if (pFlags.Has(@"\HasChildren")) lListFlags |= fListFlags.haschildren;
                        if (pFlags.Has(@"\HasNoChildren")) lListFlags |= fListFlags.hasnochildren;
                    }

                    if (mCapability.ListExtended && pExtendedItems != null)
                    {
                        foreach (var lItem in pExtendedItems)
                        {
                            if (lItem.Tag.Equals("childinfo", StringComparison.InvariantCultureIgnoreCase))
                            {
                                lListFlags |= fListFlags.haschildren;

                                if (lItem.Value.Contains("subscribed", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    lLSubFlags |= fLSubFlags.hassubscribedchildren;
                                    break;
                                }
                            }
                        }
                    }

                    // the special-use capability is to do with support by list-extended, not to do with the return of the attributes
                    if (pFlags.Has(@"\All")) lListFlags |= fListFlags.all;
                    if (pFlags.Has(@"\Archive")) lListFlags |= fListFlags.archive;
                    if (pFlags.Has(@"\Drafts")) lListFlags |= fListFlags.drafts;
                    if (pFlags.Has(@"\Flagged")) lListFlags |= fListFlags.flagged;
                    if (pFlags.Has(@"\Junk")) lListFlags |= fListFlags.junk;
                    if (pFlags.Has(@"\Sent")) lListFlags |= fListFlags.sent;
                    if (pFlags.Has(@"\Trash")) lListFlags |= fListFlags.trash;

                    rListFlags = new cListFlags(mSequence++, lListFlags);

                    if (mCapability.ListExtended) rLSubFlags = new cLSubFlags(mSequence++, lLSubFlags);
                    else rLSubFlags = null;
                }

                private void ZProcessDataLSubMailboxFlags(cFlags pFlags, out cLSubFlags rLSubFlags)
                {
                    fLSubFlags lLSubFlags;

                    if (pFlags.Has(@"\Noselect")) lLSubFlags = fLSubFlags.hassubscribedchildren;
                    else lLSubFlags = fLSubFlags.subscribed;

                    rLSubFlags = new cLSubFlags(mSequence++, lLSubFlags);
                }

                private bool ZProcessDataStatusAttributes(cBytesCursor pCursor,  out cStatus rStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessStatusAttributes));

                    uint? lMessages = null;
                    uint? lRecent = null;
                    uint? lUIDNext = null;
                    uint? lUIDValidity = null;
                    uint? lUnseen = null;
                    ulong? lHighestModSeq = null;

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
                            if (!mCapability.CondStore) lHighestModSeq = 0;
                            rStatus = new cStatus(mSequence++, lMessages, lRecent, lUIDNext, lUIDValidity, lUnseen, lHighestModSeq);
                            return true;
                        }
                    }
                }

                private static eProcessStatusAttributeResult ZProcessDataStatusAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref uint? rNumber, cTrace.cContext pParentContext)
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

                private static eProcessStatusAttributeResult ZProcessDataStatusAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref ulong? rNumber, cTrace.cContext pParentContext)
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