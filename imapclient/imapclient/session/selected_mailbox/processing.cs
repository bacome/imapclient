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
            private partial class cSelectedMailbox
            {
                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");

                private static readonly cBytes kPermanentFlagsSpace = new cBytes("PERMANENTFLAGS ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kReadWriteRBracketSpace = new cBytes("READ-WRITE] ");
                private static readonly cBytes kReadOnlyRBracketSpace = new cBytes("READ-ONLY] ");

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessData));

                    if (pCursor.SkipBytes(kFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got flags: {0}", lFlags);
                            mMailboxCacheItem.SetMessageFlags(new cMessageFlags(lFlags), lContext);
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed flags response");
                        return eProcessDataResult.notprocessed;
                    }

                    return mCache.ProcessData(pCursor, lContext);
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessTextCode));

                    if (pCursor.SkipBytes(kPermanentFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got permanentflags: {0}", lFlags);
                            mMailboxCacheItem.SetPermanentFlags(mSelectedForUpdate, new cMessageFlags(lFlags), lContext);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed permanentflags response");
                        return false;
                    }

                    if (pCursor.SkipBytes(kUIDValiditySpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidvalidity: {0}", lNumber);
                            mCache = new cSelectedMailboxMessageCache(mCache, lNumber, lContext);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidvalidity response");
                    }

                    if (pCursor.SkipBytes(kReadWriteRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-write");

                        if (mAccessReadOnly)
                        {
                            mAccessReadOnly = false;
                            mEventSynchroniser.FireMailboxPropertiesChanged(mMailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                        }

                        return true;
                    }

                    if (pCursor.SkipBytes(kReadOnlyRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-only");

                        if (!mAccessReadOnly)
                        {
                            mAccessReadOnly = true;
                            mEventSynchroniser.FireMailboxPropertiesChanged(mMailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                        }

                        return true;
                    }

                    return mCache.ProcessTextCode(pCursor, lContext);
                }
            }

            private partial class cSelectedMailboxMessageCache
            {
                /*
                private static readonly cBytes kExists = new cBytes("EXISTS");
                private static readonly cBytes kRecent = new cBytes("RECENT"); */

                private static readonly cBytes kExpunge = new cBytes("EXPUNGE");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ProcessData));

                    if (pCursor.Parsed)
                    {
                        cResponseDataFetch lFetch = pCursor.ParsedAs as cResponseDataFetch;
                        if (lFetch == null) return eProcessDataResult.notprocessed;
                        ZFetch(lFetch, lContext);
                        return eProcessDataResult.processed;
                    }

                    if (pCursor.GetNumber(out _, out var lNumber) && pCursor.SkipByte(cASCII.SPACE))
                    {
                        if (pCursor.SkipBytes(kExists))
                        {
                            if (pCursor.Position.AtEnd) 
                            {
                                lContext.TraceVerbose("got exists: {0}", lNumber);
                                ZExists((int)lNumber, lContext);
                                return eProcessDataResult.processed;
                            }

                            lContext.TraceWarning("likely malformed exists response");
                            return eProcessDataResult.notprocessed;
                        }

                        if (pCursor.SkipBytes(kRecent))
                        {
                            if (pCursor.Position.AtEnd)
                            {
                                lContext.TraceVerbose("got recent: {0}", lNumber);
                                ZRecent((int)lNumber, lContext);
                                return eProcessDataResult.processed;
                            }

                            lContext.TraceWarning("likely malformed recent response");
                            return eProcessDataResult.notprocessed;
                        }

                        if (pCursor.SkipBytes(kExpunge))
                        {
                            if (pCursor.Position.AtEnd && lNumber > 0)
                            {
                                lContext.TraceVerbose("got expunge: {0}", lNumber);
                                ZExpunge((int)lNumber, lContext);
                                return eProcessDataResult.processed;
                            }

                            lContext.TraceWarning("likely malformed expunge response");
                            return eProcessDataResult.notprocessed;
                        }

                        if (pCursor.SkipBytes(kFetchSpace))
                        {
                            if (lNumber == 0 || !cResponseDataFetch.Process(pCursor, lNumber, out var lFetch, lContext))
                            {
                                lContext.TraceWarning("likely malformed fetch response");
                                return eProcessDataResult.notprocessed;
                            }

                            ZFetch(lFetch, lContext);
                            return eProcessDataResult.observed;
                        }
                    }

                    return eProcessDataResult.notprocessed;
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ProcessTextCode));

                    if (pCursor.SkipBytes(kUIDNextSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidnext: {0}", lNumber);
                            ZUIDNext(lNumber, lContext);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidnext response");
                        return false;
                    }

                    if (pCursor.SkipBytes(kHighestModSeqSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got highestmodseq: {0}", lNumber);
                            ZHighestModSeq(lNumber, lContext);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed highestmodseq response");
                        return false;
                    }

                    return false;
                }
            }
        }
    }
}
