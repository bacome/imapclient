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
                            MailboxCacheItem.SetMessageFlags(new cMessageFlags(lFlags), lContext);
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed flags response");
                        return eProcessDataResult.notprocessed;
                    }

                    return mMessageCache.ProcessData(pCursor, lContext);
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessTextCode));

                    if (pCursor.SkipBytes(kPermanentFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got permanentflags: {0}", lFlags);
                            MailboxCacheItem.SetPermanentFlags(mSelectedForUpdate, new cMessageFlags(lFlags), lContext);
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
                            mMessageCache = new cSelectedMailboxMessageCache(mMessageCache, lNumber, lContext);
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
                            mEventSynchroniser.FireMailboxPropertiesChanged(MailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                        }

                        return true;
                    }

                    if (pCursor.SkipBytes(kReadOnlyRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-only");

                        if (!mAccessReadOnly)
                        {
                            mAccessReadOnly = true;
                            mEventSynchroniser.FireMailboxPropertiesChanged(MailboxCacheItem, fMailboxProperties.isaccessreadonly, lContext);
                        }

                        return true;
                    }

                    return mMessageCache.ProcessTextCode(pCursor, lContext);
                }
            }

            private partial class cSelectedMailboxMessageCache
            {
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");



            }
        }
    }
}
