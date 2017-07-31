using System;
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

                    if (pCursor.Parsed)
                    {
                        cResponseDataList lList = pCursor.ParsedAs as cResponseDataList;

                        if (lList != null)
                        {
                            ZProcessList(lList, lContext);
                            return eProcessDataResult.observed;
                        }

                        cResponseDataLSub lLSub = pCursor.ParsedAs as cResponseDataLSub;

                        if (lLSub != null)
                        { 
                            ZProcessLSub(lLSub, lContext);
                            return eProcessDataResult.observed;
                        }

                        return eProcessDataResult.notprocessed;
                    }

                    if (pCursor.SkipBytes(kListSpace))
                    {
                        if (!cResponseDataList.Process(pCursor, mCommandPartFactory.UTF8Enabled, out var lList, lContext)) return eProcessDataResult.notprocessed;
                        ZProcessList(lList, lContext);
                        return eProcessDataResult.observed;
                    }

                    if (pCursor.SkipBytes(kLSubSpace))
                    {
                        if (!cResponseDataLSub.Process(pCursor, mCommandPartFactory.UTF8Enabled, out var lLSub, lContext)) return eProcessDataResult.notprocessed;
                        ZProcessLSub(lLSub, lContext);
                        return eProcessDataResult.observed;
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
                        if (!ReferenceEquals(mSelectedMailbox?.Handle, lItem)) lItem.UpdateMailboxStatus(lContext);

                        return eProcessDataResult.processed;
                    }

                    return eProcessDataResult.notprocessed;
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ProcessTextCode));
                    if (mSelectedMailbox != null) return mSelectedMailbox.ProcessTextCode(pCursor, lContext);
                    return false;
                }

                private void ZProcessList(cResponseDataList pList, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessList));

                    var lItem = ZItem(pList.MailboxName);

                    // list

                    fListFlags lFlags = 0;

                    if (pList.Flags.Has(@"\Noinferiors")) lFlags |= fListFlags.noinferiors | fListFlags.hasnochildren;
                    if (pList.Flags.Has(@"\Noselect")) lFlags |= fListFlags.noselect;
                    if (pList.Flags.Has(@"\Marked")) lFlags |= fListFlags.marked;
                    if (pList.Flags.Has(@"\Unmarked")) lFlags |= fListFlags.unmarked;

                    if (mCapability.ListExtended)
                    {
                        if (pList.Flags.Has(@"\NonExistent")) lFlags |= fListFlags.noselect | fListFlags.nonexistent;
                        if (pList.Flags.Has(@"\Remote")) lFlags |= fListFlags.remote;
                    }

                    if (mCapability.Children || mCapability.ListExtended)
                    {
                        if (pList.Flags.Has(@"\HasChildren")) lFlags |= fListFlags.haschildren;
                        if (pList.Flags.Has(@"\HasNoChildren")) lFlags |= fListFlags.hasnochildren;
                    }

                    // the special-use capability is to do with support by list-extended, not to do with the return of the attributes
                    if (pList.Flags.Has(@"\All")) lFlags |= fListFlags.all;
                    if (pList.Flags.Has(@"\Archive")) lFlags |= fListFlags.archive;
                    if (pList.Flags.Has(@"\Drafts")) lFlags |= fListFlags.drafts;
                    if (pList.Flags.Has(@"\Flagged")) lFlags |= fListFlags.flagged;
                    if (pList.Flags.Has(@"\Junk")) lFlags |= fListFlags.junk;
                    if (pList.Flags.Has(@"\Sent")) lFlags |= fListFlags.sent;
                    if (pList.Flags.Has(@"\Trash")) lFlags |= fListFlags.trash;

                    lItem.SetListFlags(new cListFlags(mSequence++, lFlags), lContext);

                    // extended list also sets the subscribed flag

                    if (mCapability.ListExtended)
                    {
                        lItem.SetLSubFlags(new cLSubFlags(mSequence++, pList.Flags.Has(@"\Subscribed")), lContext);
                    }
                }

                private void ZProcessLSub(cResponseDataLSub pLSub, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessLSub));
                    var lItem = ZItem(pLSub.MailboxName);
                    lItem.SetLSubFlags(new cLSubFlags(mSequence++, !pLSub.Flags.Has(@"\Noselect")), lContext);
                }

                private bool ZProcessDataStatusAttributes(cBytesCursor pCursor,  out cStatus rStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessDataStatusAttributes));

                    uint? lMessages = null;
                    uint? lRecent = null;
                    uint? lUIDNext = null;
                    uint? lUIDValidity = null;
                    uint? lUnseen = null;
                    ulong? lHighestModSeq = null;

                    while (true)
                    {
                        eProcessStatusAttributeResult lResult;

                        lResult = ZProcessDataStatusAttribute(pCursor, kMessagesSpace, ref lMessages, lContext);

                        if (lResult == eProcessStatusAttributeResult.notprocessed)
                        {
                            lResult = ZProcessDataStatusAttribute(pCursor, kRecentSpace, ref lRecent, lContext);

                            if (lResult == eProcessStatusAttributeResult.notprocessed)
                            {
                                lResult = ZProcessDataStatusAttribute(pCursor, kUIDNextSpace, ref lUIDNext, lContext);

                                if (lResult == eProcessStatusAttributeResult.notprocessed)
                                {
                                    lResult = ZProcessDataStatusAttribute(pCursor, kUIDValiditySpace, ref lUIDValidity, lContext);

                                    if (lResult == eProcessStatusAttributeResult.notprocessed)
                                    {
                                        lResult = ZProcessDataStatusAttribute(pCursor, kUnseenSpace, ref lUnseen, lContext);

                                        if (lResult == eProcessStatusAttributeResult.notprocessed) lResult = ZProcessDataStatusAttribute(pCursor, kHighestModSeqSpace, ref lHighestModSeq, lContext);
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
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessDataStatusAttribute), pAttributeSpace);

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
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessDataStatusAttribute));

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