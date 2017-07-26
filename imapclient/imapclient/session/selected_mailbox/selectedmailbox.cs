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
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kReadWriteRBracketSpace = new cBytes("READ-WRITE] ");
                private static readonly cBytes kReadOnlyRBracketSpace = new cBytes("READ-ONLY] ");

                private readonly cEventSynchroniser mEventSynchroniser;
                public readonly cMailboxCacheItem MailboxCacheItem;
                private readonly bool mSelectedForUpdate;

                private bool mAccessReadOnly = false; // can change
                private cSelectedMailboxMessageCache mMessageCache = null;

                public cSelectedMailbox(cEventSynchroniser pEventSynchoniser, cMailboxCacheItem pMailboxCacheItem, bool pSelectedForUpdate, bool pAccessReadOnly, int pExists, int pRecent, uint pUIDNext, uint pUIDValidity, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailbox), pMailboxCacheItem, pSelectedForUpdate, pAccessReadOnly, pExists, pRecent, pUIDNext, pUIDValidity, pHighestModSeq);
                    mEventSynchroniser = pEventSynchoniser ?? throw new ArgumentNullException(nameof(pEventSynchoniser));
                    MailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mSelectedForUpdate = pSelectedForUpdate;
                    mAccessReadOnly = pAccessReadOnly;
                    mMessageCache = new cSelectedMailboxMessageCache(pEventSynchoniser, pMailboxCacheItem, pUIDValidity, pExists, pRecent, pUIDNext, pHighestModSeq, lContext);
                }

                public iMailboxHandle Handle => MailboxCacheItem;
                public bool SelectedForUpdate => mSelectedForUpdate;
                public bool AccessReadOnly => mAccessReadOnly;

                public iMessageHandle GetHandle(uint pMSN) => mMessageCache.GetHandle(pMSN); // this should only be called from a commandcompletion
                public iMessageHandle GetHandle(cUID pUID) => mMessageCache.GetHandle(pUID);
                public uint GetMSN(iMessageHandle pHandle) => mMessageCache.GetMSN(pHandle); // this should only be called when no msnunsafe commands are running

                public void SetUnseenBegin(cTrace.cContext pParentContext) => mMessageCache.SetUnseenBegin(pParentContext);
                public void SetUnseen(cUIntList pMSNs, cTrace.cContext pParentContext) => mMessageCache.SetUnseen(pMSNs, pParentContext); // this should only be called from a commandcompletion

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => mMessageCache.ProcessData(pCursor, pParentContext);

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