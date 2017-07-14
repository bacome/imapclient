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
            private class cStatusDataProcessor : iUnsolicitedDataProcessor
            {
                private enum eProcessAttributeResult { notprocessed, processed, error }

                private static readonly cBytes kStatusSpace = new cBytes("STATUS ");
                private static readonly cBytes kMessagesSpace = new cBytes("MESSAGES ");
                private static readonly cBytes kRecentSpace = new cBytes("RECENT ");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");

                private readonly cMailboxCache mMailboxCache;

                public cStatusDataProcessor(cMailboxCache pMailboxCache)
                {
                    mMailboxCache = pMailboxCache;
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusDataProcessor), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kStatusSpace)) return eProcessDataResult.notprocessed;

                    if (!pCursor.GetAString(out string lEncodedMailboxName) ||
                        !pCursor.SkipBytes(cBytesCursor.SpaceLParen) ||
                        !ZProcessAttributes(pCursor, out var lStatus, lContext) ||
                        !pCursor.SkipByte(cASCII.RPAREN) ||
                        !pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed status response");
                        return eProcessDataResult.notprocessed;
                    }

                    mMailboxCache.UpdateMailboxStatus(lEncodedMailboxName, lStatus, lContext);

                    return eProcessDataResult.processed;
                }

                private static bool ZProcessAttributes(cBytesCursor pCursor, out cStatus rStatus, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusDataProcessor), nameof(ZProcessAttributes));

                    int? lMessages = 0;
                    int? lRecent = 0;
                    uint? lUIDNext = 0;
                    uint? lUIDValidity = 0;
                    int? lUnseen = 0;
                    ulong? lHighestModSeq = 0;

                    while (true)
                    {
                        eProcessAttributeResult lResult;

                        lResult = ZProcessAttribute(pCursor, kMessagesSpace, ref lMessages, lContext);

                        if (lResult == eProcessAttributeResult.notprocessed)
                        {
                            lResult = ZProcessAttribute(pCursor, kRecentSpace, ref lRecent, lContext);

                            if (lResult == eProcessAttributeResult.notprocessed)
                            {
                                lResult = ZProcessAttribute(pCursor, kUIDNextSpace, ref lUIDNext, lContext);

                                if (lResult == eProcessAttributeResult.notprocessed)
                                {
                                    lResult = ZProcessAttribute(pCursor, kUIDValiditySpace, ref lUIDValidity, lContext);

                                    if (lResult == eProcessAttributeResult.notprocessed)
                                    {
                                        lResult = ZProcessAttribute(pCursor, kUnseenSpace, ref lUnseen, lContext);

                                        if (lResult == eProcessAttributeResult.notprocessed) lResult = ZProcessHighestModSeq(pCursor, ref lHighestModSeq, lContext);
                                    }
                                }
                            }
                        }

                        if (lResult != eProcessAttributeResult.processed)
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

                private static eProcessAttributeResult ZProcessAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref int? rNumber, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusDataProcessor), nameof(ZProcessAttribute), pAttributeSpace);

                    if (!pCursor.SkipBytes(pAttributeSpace)) return eProcessAttributeResult.notprocessed;

                    if (pCursor.GetNumber(out _, out var lNumber))
                    {
                        lContext.TraceVerbose("got {0}", lNumber);
                        rNumber = (int)lNumber;
                        return eProcessAttributeResult.processed;
                    }

                    lContext.TraceWarning("likely malformed status-att-list-item: no number?");
                    return eProcessAttributeResult.error;
                }

                private static eProcessAttributeResult ZProcessAttribute(cBytesCursor pCursor, cBytes pAttributeSpace, ref uint? rNumber, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusDataProcessor), nameof(ZProcessAttribute), pAttributeSpace);

                    if (!pCursor.SkipBytes(pAttributeSpace)) return eProcessAttributeResult.notprocessed;

                    if (pCursor.GetNumber(out _, out var lNumber))
                    {
                        lContext.TraceVerbose("got {0}", lNumber);
                        rNumber = lNumber;
                        return eProcessAttributeResult.processed;
                    }

                    lContext.TraceWarning("likely malformed status-att-list-item: no number?");
                    return eProcessAttributeResult.error;
                }

                private static eProcessAttributeResult ZProcessHighestModSeq(cBytesCursor pCursor, ref ulong? rModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusDataProcessor), nameof(ZProcessHighestModSeq));

                    if (!pCursor.SkipBytes(kHighestModSeqSpace)) return eProcessAttributeResult.notprocessed;

                    if (pCursor.GetModSeq(out var lModSeq))
                    {
                        lContext.TraceVerbose("got {0}", lModSeq);
                        rModSeq = lModSeq;
                        return eProcessAttributeResult.processed;
                    }

                    lContext.TraceWarning("likely malformed status-att-list-item: no number?");
                    return eProcessAttributeResult.error;
                }
            }
        }
    }
}