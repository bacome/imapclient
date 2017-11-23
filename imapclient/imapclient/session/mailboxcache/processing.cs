using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache
            {
                private static readonly cBytes kStatusSpace = new cBytes("STATUS ");
                private static readonly cBytes kMessagesSpace = new cBytes("MESSAGES ");
                private static readonly cBytes kRecentSpace = new cBytes("RECENT ");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");

                private enum eProcessStatusAttributeResult { notprocessed, processed, error }

                public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ProcessData));

                    if (mSelectedMailbox != null)
                    {
                        var lResult = mSelectedMailbox.ProcessData(pData, lContext);
                        if (lResult != eProcessDataResult.notprocessed) return lResult;
                    }

                    switch (pData)
                    {
                        case cResponseDataListMailbox lListMailbox:

                            ZProcessListMailbox(lListMailbox, lContext);
                            return eProcessDataResult.observed;

                        case cResponseDataLSub lLSub:

                            ZProcessLSub(lLSub, lContext);
                            return eProcessDataResult.observed;

                        default:

                            return eProcessDataResult.notprocessed;
                    }
                }

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

                    if (pCursor.SkipBytes(kStatusSpace))
                    {
                        if (!pCursor.GetAString(out string lEncodedMailboxPath) ||
                            !pCursor.SkipBytes(cBytesCursor.SpaceLParen) ||
                            !ZProcessDataStatusAttributes(pCursor, out var lStatus, lContext) ||
                            !pCursor.SkipByte(cASCII.RPAREN) ||
                            !pCursor.Position.AtEnd)
                        {
                            lContext.TraceWarning("likely malformed status response");
                            return eProcessDataResult.notprocessed;
                        }

                        var lItem = ZItem(lEncodedMailboxPath);
                        lItem.UpdateStatus(lStatus, lContext);
                        if (!ReferenceEquals(mSelectedMailbox?.MailboxHandle, lItem)) lItem.UpdateMailboxStatus(lContext);

                        return eProcessDataResult.processed;
                    }

                    return eProcessDataResult.notprocessed;
                }

                public void ProcessTextCode(eResponseTextContext pTextContext, cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ProcessTextCode), pTextContext, pData);
                    if (mSelectedMailbox != null) mSelectedMailbox.ProcessTextCode(pTextContext, pData, lContext);
                }

                private void ZProcessListMailbox(cResponseDataListMailbox pListMailbox, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessListMailbox));

                    var lItem = ZItem(pListMailbox.MailboxName);
                    lItem.SetListFlags(new cListFlags(mSequence++, pListMailbox.Flags), lContext);

                    if (mCapabilities.ListExtended)
                    {
                        if ((mMailboxCacheDataItems & fMailboxCacheDataItems.subscribed) != 0) lItem.SetLSubFlags(new cLSubFlags(mSequence++, (pListMailbox.Flags & fListFlags.subscribed) != 0), lContext);
                        else if ((pListMailbox.Flags & fListFlags.subscribed) != 0) lItem.SetLSubFlags(new cLSubFlags(mSequence++, true), lContext);
                    }
                }

                private void ZProcessLSub(cResponseDataLSub pLSub, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMailboxCache), nameof(ZProcessLSub));
                    var lItem = ZItem(pLSub.MailboxName);
                    lItem.SetLSubFlags(new cLSubFlags(mSequence++, pLSub.Subscribed), lContext);
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

                                        if (lResult == eProcessStatusAttributeResult.notprocessed)
                                        {
                                            lResult = ZProcessDataStatusAttribute(pCursor, kHighestModSeqSpace, ref lHighestModSeq, lContext);
                                        }
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
                            if (!mCapabilities.CondStore) lHighestModSeq = 0;
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