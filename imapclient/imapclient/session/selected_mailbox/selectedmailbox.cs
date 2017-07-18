using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailbox : iSelectedMailboxDetails
            {
                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kPermanentFlagsSpace = new cBytes("PERMANENTFLAGS ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kReadWriteRBracketSpace = new cBytes("READ-WRITE] ");
                private static readonly cBytes kReadOnlyRBracketSpace = new cBytes("READ-ONLY] ");
                ;?;

                private readonly cMailboxCache mMailboxCache;
                private readonly iMailboxHandle mHandle;
                private readonly bool mSelectedForUpdate;
                private readonly bool mCondStoreRequested;
                private readonly cEventSynchroniser mEventSynchroniser;

                private cMessageCache mMessageCache;

                private cMessageFlags mMessageFlags = null;
                private cMessageFlags mPermanentFlags = null;
                private bool mAccessReadOnly = false;
                ??private bool mCondStoreEnabled = false; // true if condstore was asked for and we see a highestmodseq before setasselected is called

                ;?; no longer required
                //private bool mHasBeenSetAsSelected = false;

                public cSelectedMailbox(cMailboxCache pMailboxCache, iMailboxHandle pHandle, bool pSelectedForUpdate, bool pCondStoreRequested, cEventSynchroniser pEventSynchoniser)
                {
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    mHandle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
                    mSelectedForUpdate = pSelectedForUpdate;
                    mCondStoreRequested = pCondStoreRequested;
                    mEventSynchroniser = pEventSynchoniser ?? throw new ArgumentNullException(nameof(pEventSynchoniser));

                    mMessageCache = new cMessageCache(pMailboxCache, pHandle, 0, false, pEventSynchoniser, ); ???
                }

                public iMailboxHandle Handle => mHandle;
                public bool SelectedForUpdate => mSelectedForUpdate;
                public bool AccessReadOnly => mAccessReadOnly;

                public void SetAsSelected(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(SetAsSelected));
                    if (mHasBeenSetAsSelected) throw new InvalidOperationException();
                    mHasBeenSetAsSelected = true;
                    mMessageCache.SetAsSelected(lContext);
                    mMailboxCache.UpdateMailboxBeenSelected(mEncodedMailboxName, mMailboxId.MailboxName, mMessageFlags, mSelectedForUpdate, mPermanentFlags, lContext);
                }

                public bool CondStoreEnabled => mCondStoreEnabled;

                public iMessageHandle GetHandle(uint pMSN) => mMessageCache.GetHandle(pMSN); // this should only be called from a commandcompletion
                public iMessageHandle GetHandle(cUID pUID) => mMessageCache.GetHandle(pUID);
                public uint GetMSN(iMessageHandle pHandle) => mMessageCache.GetMSN(pHandle); // this should only be called when no msnunsafe commands are running

                public int SetUnseenBegin(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(SetUnseenBegin));
                    int lCount = mMessageCache.MessageCount;
                    mMessageCache.SetUnseenCount = lCount;
                    return lCount;
                }

                public void SetUnseen(cUIntList pMSNs, cTrace.cContext pParentContext) => mMessageCache.SetUnseen(pMSNs, pParentContext); // this should only be called from a commandcompletion

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessData));

                    eProcessDataResult lResult;

                    var lBookmark = pCursor.Position;
                    lResult = mMessageCache.ProcessData(pCursor, lContext);
                    if (lResult != eProcessDataResult.notprocessed) return lResult;
                    pCursor.Position = lBookmark;

                    if (pCursor.SkipBytes(kFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got flags available for messages in the mailbox: {0}", lFlags);
                            mMessageFlags = new cMessageFlags(lFlags);
                            if (mHasBeenSetAsSelected) mMailboxCache.UpdateMailboxBeenSelected(mEncodedMailboxName, mMailboxId.MailboxName, mMessageFlags, mSelectedForUpdate, mPermanentFlags, lContext);
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed flags response");
                    }

                    return eProcessDataResult.notprocessed;
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessTextCode));

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

                    return mCache.ProcessTextCode(pCursor, lContext);
                }

                public override string ToString() => $"{nameof(cSelectedMailbox)}({mMailboxId},{mSelectedForUpdate})";
            }
        }
    }
}