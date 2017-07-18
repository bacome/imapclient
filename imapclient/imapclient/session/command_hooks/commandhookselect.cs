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
                private static readonly cBytes kReadWriteRBracketSpace = new cBytes("READ-WRITE] ");
                private static readonly cBytes kReadOnlyRBracketSpace = new cBytes("READ-ONLY] ");

                private bool mDeselectRequired;
                private readonly cCapability mCapability;
                private readonly Action<cSelectedMailbox, cTrace.cContext> mSetSelectedMailbox;

                private cMessageFlags mFlags = null;
                private int mExists = 0;
                private int mRecent = 0;
                private cMessageFlags mPermanentFlags = null;
                private uint mUIDNext = 0;
                private uint mUIDValidity = 0;
                private uint mHighestModSeq = 0;
                private bool mAccessReadOnly = false;

                public cCommandHookSelect(bool pDeselectRequired, cCapability pCapability, bool pSelectedForUpdate, Action<cSelectedMailbox, cTrace.cContext> pSetSelectedMailbox)
                {
                    mDeselectRequired = pDeselectRequired;
                    mCapability = pCapability ?? throw new ArgumentNullException(nameof(pCapability));
                    mSetSelectedMailbox = pSetSelectedMailbox ?? throw new ArgumentNullException(nameof(pSetSelectedMailbox));
                }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandStarted));

                    if (mDeselectRequired && !mCapability.QResync)
                    {
                        mDeselectRequired = false;
                        mSetSelectedMailbox(null, lContext);
                    }
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(ProcessData));

                    if (mDeselectRequired) return eProcessDataResult.notprocessed;

                    if (pCursor.SkipBytes(kFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got flags available for messages in the mailbox: {0}", lFlags);
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
                                lContext.TraceVerbose("got the number of messages in the mailbox: {0}", lNumber);
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
                                lContext.TraceVerbose("got the number of recent messages in the mailbox: {0}", lNumber);
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

                    if (mDeselectRequired)
                    {
                        if (pCursor.SkipBytes(kClosedRBracketSpace))
                        {
                            lContext.TraceVerbose("got closed");
                            mDeselectRequired = false;
                            mSetSelectedMailbox(null, lContext);
                            return true;
                        }

                        return false;
                    }

                    ;?;








                    if (pCursor.SkipBytes(kUnseenSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got unseen: {0}", lNumber);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed unseen response");
                    }

                    if (pCursor.SkipBytes(kPermanentFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got permanentflags: {0}", lFlags);
                            mPermanentFlags = new cMessageFlags(lFlags);
                            if (mHasBeenSetAsSelected) mMailboxCache.UpdateMailboxBeenSelected(mEncodedMailboxName, mMailboxId.MailboxName, mMessageFlags, mSelectedForUpdate, mPermanentFlags, lContext);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed permanentflags response");
                    }

                    if (pCursor.SkipBytes(kUIDValiditySpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidvalidity: {0}", lNumber);
                            mMessageCache = new cMessageCache(mMessageCache, lNumber);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidvalidity response");
                    }

                    if (pCursor.SkipBytes(kReadWriteRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-write");
                        mAccessReadOnly = false;
                        if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.AccessReadOnly), lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kReadOnlyRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-only");
                        AccessReadOnly = true;
                        if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.AccessReadOnly), lContext);
                        return true;
                    }

                    ;?; // highest mod seq

                    ;?; // nomodseq



















                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSelect), nameof(CommandCompleted), pResult);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok)
                    {
                        ;?; // this is where the cache is created/updated
                        ;?; // this is where rthe new mailbox is created
                        mSetSelectedMailbox(mPendingSelectedMailbox, lContext);
                    }
                }
            }
        }
    }
}