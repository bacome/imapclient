using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailbox : iMailboxProperties
            {
                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");
                private static readonly cBytes kExists = new cBytes("EXISTS");
                private static readonly cBytes kRecent = new cBytes("RECENT");
                private static readonly cBytes kUnseenSpace = new cBytes("UNSEEN ");
                private static readonly cBytes kPermanentFlagsSpace = new cBytes("PERMANENTFLAGS ");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kUIDValiditySpace = new cBytes("UIDVALIDITY ");
                private static readonly cBytes kReadWriteRBracketSpace = new cBytes("READ-WRITE] ");
                private static readonly cBytes kReadOnlyRBracketSpace = new cBytes("READ-ONLY] ");

                public readonly cMailboxId MailboxId;

                private readonly bool mSelectedForUpdate;
                private readonly cEventSynchroniser mEventSynchroniser;
                private dGetCapability mGetCapability;
                private bool mHasBeenSetAsSelected = false;

                private cMessageFlags mPermanentFlags = null;

                private uint? mUnseen = null;
                private cCache mCache;

                public cSelectedMailbox(cMailboxId pMailboxId, bool pForUpdate, cEventSynchroniser pEventSynchoniser, dGetCapability pGetCapability)
                {
                    MailboxId = pMailboxId ?? throw new ArgumentNullException(nameof(pMailboxId));
                    mSelectedForUpdate = pForUpdate;
                    mEventSynchroniser = pEventSynchoniser ?? throw new ArgumentNullException(nameof(pEventSynchoniser));
                    mGetCapability = pGetCapability;
                    mCache = new cCache(pMailboxId, null, pEventSynchoniser, mGetCapability, false);
                }

                public void SetAsSelected(cTrace.cContext pParentContext)
                {
                    // called when the mailbox is first set as the selected mailbox
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(SetAsSelected));
                    if (mHasBeenSetAsSelected) throw new InvalidOperationException();
                    mHasBeenSetAsSelected = true;
                    mCache.SetAsSelected(lContext);
                }

                public cMessageFlags Flags { get; private set; } = null;
                public cMessageFlags PermanentFlags => mPermanentFlags ?? Flags;

                public int Messages => mCache.Count;
                public int? Recent { get; private set; }
                public uint? UIDNext { get; private set; }
                public uint? UIDValidity => mCache.UIDValidity;

                public int? Unseen
                {
                    get
                    {
                        if (mCache.UnseenNull > 0) return null;
                        else return mCache.UnseenTrue;
                    }
                }

                /*
                public cMailboxStatus Status
                {
                    get
                    {
                        return new cMailboxStatus((uint)mCache.Count, mRecent, mUIDNext, UIDValidity, lUnseen);
                    }
                }*/

                public bool Selected => true;
                public bool SelectedForUpdate => mSelectedForUpdate;
                public bool AccessReadOnly { get; private set; } = false;

                public iMessageHandle GetHandle(uint pMSN) => mCache.GetHandle(pMSN); // this should only be called from a commandcompletion
                public iMessageHandle GetHandle(cUID pUID) => mCache.GetHandle(pUID);
                public uint GetMSN(iMessageHandle pHandle) => mCache.GetMSN(pHandle); // this should only be called when no msnunsafe commands are running

                public int SetUnseenBegin(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(SetUnseenBegin));
                    int lCount = mCache.Count;
                    mCache.SetUnseenCount = lCount;
                    return lCount;
                }

                public int SetUnseen(cUIntList pMSNs, cTrace.cContext pParentContext) => mCache.SetUnseen(pMSNs, pParentContext); // this should only be called from a commandcompletion

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailbox), nameof(ProcessData));

                    eProcessDataResult lResult;

                    var lBookmark = pCursor.Position;
                    lResult = mCache.ProcessData(pCursor, lContext);
                    if (lResult != eProcessDataResult.notprocessed) return lResult;
                    pCursor.Position = lBookmark;

                    if (pCursor.SkipBytes(kFlagsSpace))
                    {
                        if (pCursor.GetFlags(out var lFlags) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got flags available for messages in the mailbox: {0}", lFlags);
                            Flags = new cMessageFlags(lFlags);
                            if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.Flags), lContext); // may also mean that permanent flags have changed
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed flags response");
                    }

                    if (pCursor.GetNumber(out _, out var lNumber) && pCursor.SkipByte(cASCII.SPACE))
                    {
                        if (pCursor.SkipBytes(kExists))
                        {
                            if (pCursor.Position.AtEnd)
                            {
                                mCache.IncreaseCount((int)lNumber, lContext);
                                if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.Messages), lContext);
                                return eProcessDataResult.processed;
                            }
                        }
                        else if (pCursor.SkipBytes(kRecent))
                        {
                            if (pCursor.Position.AtEnd)
                            {
                                lContext.TraceVerbose("got recent: {0}", lNumber);
                                Recent = (int)lNumber;
                                if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.Recent), lContext);
                                return eProcessDataResult.processed;
                            }
                        }
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
                            mUnseen = lNumber;
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
                            if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.PermanentFlags), lContext);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed permanentflags response");
                    }

                    if (pCursor.SkipBytes(kUIDNextSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidnext: {0}", lNumber);
                            UIDNext = lNumber;
                            if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.UIDNext), lContext);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidnext response");
                    }

                    if (pCursor.SkipBytes(kUIDValiditySpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidvalidity: {0}", lNumber);

                            var lOldCache = mCache;

                            mCache = new cCache(MailboxId, lNumber, mEventSynchroniser, mGetCapability, mHasBeenSetAsSelected);
                            mCache.IncreaseCount(lOldCache.Count, lContext);
                            mCache.SetUnseenCount = lOldCache.SetUnseenCount;

                            lOldCache.Invalidate(lContext);
                            if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxPropertyChanged(MailboxId, nameof(iMailboxProperties.UIDValidity), lContext);

                            return true;
                        }

                        lContext.TraceWarning("likely malformed uidvalidity response");
                    }

                    if (pCursor.SkipBytes(kReadWriteRBracketSpace))
                    {
                        lContext.TraceVerbose("got read-write");
                        AccessReadOnly = false;
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

                    return false;
                }

                ;?; // to string
            }
        }
    }
}