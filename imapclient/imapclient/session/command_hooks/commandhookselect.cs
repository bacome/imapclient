using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookSelect : cCommandHook
            {
                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");
                private static readonly cBytes kExists = new cBytes("EXISTS");
                private static readonly cBytes kRecent = new cBytes("RECENT");

                private static readonly cBytes kClosedRBracketSpace = new cBytes("CLOSED] ");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kPermanentFlagsSpace = new cBytes("PERMANENTFLAGS ");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");
                private static readonly cBytes kNoModSeqRBracketSpace = new cBytes("NOMODSEQ] ");
                private static readonly cBytes kReadWriteRBracketSpace = new cBytes("READ-WRITE] ");
                private static readonly cBytes kReadOnlyRBracketSpace = new cBytes("READ-ONLY] ");

                private readonly cMailboxCache mMailboxCache;
                private readonly cCapability mCapability;
                private readonly iMailboxHandle mHandle;
                private readonly bool mForUpdate;

                private bool mDeselectDone = false;

                private cMessageFlags mFlags = null;
                private int mExists = 0;
                private int mRecent = 0;
                private cMessageFlags mPermanentFlags = null;
                private uint mUIDNext = 0;
                private uint mUIDValidity = 0;
                private uint mHighestModSeq = 0;
                private bool mAccessReadOnly = false;

                public cCommandHookSelect(cMailboxCache pMailboxCache, cCapability pCapability, iMailboxHandle pHandle, bool pForUpdate)
                {
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    mCapability = pCapability ?? throw new ArgumentNullException(nameof(pCapability));
                    mHandle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
                    mForUpdate = pForUpdate;
                }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandStarted));

                    if (!mCapability.QResync)
                    {
                        mDeselectDone = true;
                        mMailboxCache.Deselect(lContext);
                    }
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessData));

                    if (!mDeselectDone) return eProcessDataResult.notprocessed;

                    if (pCursor.SkipBytes(kFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got flags: {0}", lFlags);
                            mFlags = new cMessageFlags(lFlags);
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed flags response");
                        return eProcessDataResult.notprocessed;
                    }

                    if (pCursor.GetNumber(out _, out var lNumber) && pCursor.SkipByte(cASCII.SPACE))
                    {
                        if (pCursor.SkipBytes(kExists))
                        {
                            if (pCursor.Position.AtEnd)
                            {
                                lContext.TraceVerbose("got exists: {0}", lNumber);
                                mExists = (int)lNumber;
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
                                mRecent = (int)lNumber;
                                return eProcessDataResult.processed;
                            }

                            lContext.TraceWarning("likely malformed recent response");
                            return eProcessDataResult.notprocessed;
                        }
                    }

                    return eProcessDataResult.notprocessed;
                }

                public override bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessTextCode));

                    if (!mDeselectDone)
                    {
                        if (pCursor.SkipBytes(kClosedRBracketSpace))
                        {
                            lContext.TraceVerbose("got closed");
                            mDeselectDone = true;
                            mMailboxCache.Deselect(lContext);
                            return true;
                        }

                        return false;
                    }

                    if (pCursor.SkipBytes(kUnseenSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got unseen: {0}", lNumber);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed unseen response");
                        return false;
                    }

                    if (pCursor.SkipBytes(kPermanentFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got permanentflags: {0}", lFlags);
                            mPermanentFlags = new cMessageFlags(lFlags);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed permanentflags response");
                        return false;
                    }

                    if (pCursor.SkipBytes(kUIDNextSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidnext: {0}", lNumber);
                            mUIDNext = lNumber;
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidnext response");
                        return false;
                    }

                    if (pCursor.SkipBytes(kUIDValiditySpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidvalidity: {0}", lNumber);
                            mUIDValidity = lNumber;
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidvalidity response");
                        return false;
                    }

                    if (mCapability.CondStore)
                    {
                        if (pCursor.SkipBytes(kHighestModSeqSpace))
                        {
                            if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                            {
                                lContext.TraceVerbose("got highestmodseq: {0}", lNumber);
                                mHighestModSeq = lNumber;
                                return true;
                            }

                            lContext.TraceWarning("likely malformed highestmodseq response");
                            return false;
                        }

                        if (pCursor.SkipBytes(kNoModSeqRBracketSpace))
                        {
                            lContext.TraceVerbose("got nomodseq");
                            mHighestModSeq = 0;
                            return true;
                        }
                    }

                    if (pCursor.SkipBytes(kReadWriteRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-write");
                        mAccessReadOnly = false;
                        return true;
                    }

                    if (pCursor.SkipBytes(kReadOnlyRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-only");
                        mAccessReadOnly = true;
                        return true;
                    }

                    return false;
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandCompleted), pResult);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) mMailboxCache.Select(mHandle, mForUpdate, mAccessReadOnly, mFlags, mPermanentFlags, mExists, mRecent, mUIDNext, mUIDValidity, mHighestModSeq, lContext);
                }
            }
        }
    }
}