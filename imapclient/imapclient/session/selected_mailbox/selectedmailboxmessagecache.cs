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
            private partial class cSelectedMailboxMessageCache : iMessageCache
            {
                private static readonly cBytes kExists = new cBytes("EXISTS");
                private static readonly cBytes kRecent = new cBytes("RECENT");
                private static readonly cBytes kUIDNextSpace = new cBytes("UIDNEXT ");
                private static readonly cBytes kHighestModSeqSpace = new cBytes("HIGHESTMODSEQ ");
                private static readonly cBytes kExpunge = new cBytes("EXPUNGE");
                private static readonly cBytes kFetchSpace = new cBytes("FETCH ");

                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly cMailboxCacheItem mMailboxCacheItem;
                private readonly uint mUIDValidity;
                private readonly bool mNoModSeq;

                private int mCacheSequence = 0;
                private readonly List<cItem> mItems;
                private readonly SortedDictionary<cUID, iMessageHandle> mUIDIndex = new SortedDictionary<cUID, iMessageHandle>();

                private int mRecentCount;

                private uint mUIDNext;
                private int mUIDNextMessageCount;
                private int mUIDNextUnknownCount;

                private int mUnseenCount;
                private int mUnseenUnknownCount;

                private ulong mHighestModSeq;

                private int mSetUnseenCount;

                public cSelectedMailboxMessageCache(cEventSynchroniser pEventSynchroniser, cMailboxCacheItem pMailboxCacheItem, uint pUIDValidity, int pMessageCount, int pRecentCount, uint pUIDNext, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailboxMessageCache), pMailboxCacheItem, pUIDValidity, pMessageCount, pRecentCount, pUIDNext, pHighestModSeq);

                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mMailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mUIDValidity = pUIDValidity;
                    mNoModSeq = pHighestModSeq == 0;

                    mItems = new List<cItem>(pMessageCount);
                    for (int i = 0; i < pMessageCount; i++) mItems.Add(new cItem(this, mCacheSequence++));

                    mRecentCount = pRecentCount;

                    mUIDNext = pUIDNext;
                    mUIDNextMessageCount = pMessageCount;
                    mUIDNextUnknownCount = 0;

                    mUnseenCount = 0;
                    mUnseenUnknownCount = pMessageCount;

                    mHighestModSeq = pHighestModSeq;

                    mSetUnseenCount = 0;

                    ZSetMailboxStatus(lContext);
                }

                public cSelectedMailboxMessageCache(cSelectedMailboxMessageCache pOldCache, uint pUIDValidity, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailboxMessageCache), pOldCache, pUIDValidity);

                    mEventSynchroniser = pOldCache.mEventSynchroniser;
                    mMailboxCacheItem = pOldCache.mMailboxCacheItem;
                    mUIDValidity = pUIDValidity;
                    mNoModSeq = pOldCache.mNoModSeq;

                    int lMessageCount = pOldCache.mItems.Count;

                    mItems = new List<cItem>(lMessageCount);
                    for (int i = 0; i < lMessageCount; i++) mItems.Add(new cItem(this, mCacheSequence++));

                    mRecentCount = pOldCache.mRecentCount;

                    mUIDNext = 0;
                    mUIDNextMessageCount = 0;
                    mUIDNextUnknownCount = lMessageCount;

                    mUnseenCount = 0;
                    mUnseenUnknownCount = lMessageCount;

                    mHighestModSeq = 0;

                    mSetUnseenCount = pOldCache.mSetUnseenCount;

                    ZSetMailboxStatus(lContext);
                }

                public iMessageHandle GetHandle(uint pMSN) => mItems[(int)pMSN - 1];

                public iMessageHandle GetHandle(cUID pUID)
                {
                    if (mUIDIndex.TryGetValue(pUID, out var lHandle)) return lHandle;
                    return null;
                }

                public uint GetMSN(iMessageHandle pHandle)
                {
                    // this should only be called when no msnunsafe commands are running
                    //  zero return means that the message isn't cached

                    if (!(pHandle is cItem lItem)) return 0;
                    if (!ReferenceEquals(lItem.MessageCache, this)) return 0;
                    int lIndex = mItems.BinarySearch(lItem);
                    if (lIndex < 0) return 0;
                    return (uint)lIndex + 1;
                }

                public void SetUnseenBegin(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(SetUnseenBegin));
                    mSetUnseenCount = mItems.Count;
                }

                public void SetUnseen(cUIntList pMSNs, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(SetUnseen), pMSNs);

                    int lMaxIndex = mSetUnseenCount - 1;
                    bool lUnseenUpdated = false;

                    foreach (uint lMSN in pMSNs)
                    {
                        int lIndex = (int)lMSN - 1;

                        if (lIndex > lMaxIndex) lMaxIndex = lIndex;

                        var lItem = mItems[lIndex];

                        if (lItem.Unseen == null)
                        {
                            lItem.Unseen = true;
                            mUnseenUnknownCount--;
                            mUnseenCount++;
                            lUnseenUpdated = true;
                        }
                    }

                    for (int i = 0; i <= lMaxIndex; i++)
                    {
                        var lItem = mItems[i];

                        if (lItem.Unseen == null)
                        {
                            lItem.Unseen = false;
                            mUnseenUnknownCount--;
                            lUnseenUpdated = true;
                        }
                    }

                    if (lUnseenUpdated) ZSetMailboxStatus(lContext);
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ProcessData));

                    bool lUnseenUpdated = false;
                    cResponseDataFetch lFetch;

                    if (pCursor.Parsed)
                    {
                        lFetch = pCursor.ParsedAs as cResponseDataFetch;
                        if (lFetch == null) return eProcessDataResult.notprocessed;
                    }
                    else
                    {
                        ;?; // review this
                        if (pCursor.GetNumber(out _, out var lNumber) && pCursor.SkipByte(cASCII.SPACE))
                        {
                            if (pCursor.SkipBytes(kExists))
                            {
                                if (pCursor.Position.AtEnd) IncreaseCount((int)lNumber, lContext);
                                {
                                    ; ?;



                                    return eProcessDataResult.processed;
                                }
                            }
                            else if (pCursor.SkipBytes(kRecent))
                            {
                                if (pCursor.Position.AtEnd)
                                {
                                    lContext.TraceVerbose("got recent: {0}", lNumber);
                                    mRecentCount = (int)lNumber;
                                    if (mHasBeenSetAsSelected) ZUpdateMailboxStatus(lContext);
                                    return eProcessDataResult.processed;
                                }
                            }
                            else if (pCursor.SkipBytes(kExpunge))
                            {
                                if (!pCursor.Position.AtEnd)
                                {
                                    lContext.TraceWarning("likely malformed expunge response");
                                    return eProcessDataResult.notprocessed;
                                }

                                // processing

                                int lIndex = (int)lMSN - 1;
                                var lExpungedItem = mItems[lIndex];
                                mItems.RemoveAt(lIndex);

                                lExpungedItem.SetExpunged();

                                if (lExpungedItem.Unseen == null)
                                {
                                    mUnseenUnknownCount--;
                                    lUnseenUpdated = true;
                                }
                                else if (lExpungedItem.Unseen.Value)
                                {
                                    mUnseenCount--;
                                    lUnseenUpdated = true;
                                }

                                if (lIndex < SetUnseenCount) SetUnseenCount--;

                                if (mHasBeenSetAsSelected)
                                {
                                    mEventSynchroniser.MessagePropertyChanged(mMailboxId, lExpungedItem, nameof(cMessage.IsExpunged), lContext);
                                    if (lUnseenUpdated) mEventSynchroniser.MailboxPropertyChanged(mMailboxId, nameof(iMailboxProperties.Unseen), lContext);
                                    mEventSynchroniser.MailboxPropertyChanged(mMailboxId, nameof(iMailboxProperties.Messages), lContext);
                                }

                                // done
                                return eProcessDataResult.processed;
                            }
                            elese // fetch

                            ;?; // number followed by what?
                        }




                        if (!pCursor.GetNZNumber(out _, out var lMSN) || !pCursor.SkipByte(cASCII.SPACE)) return eProcessDataResult.notprocessed;

                        if (pCursor.SkipBytes(kExpunge))
                        {
                        }

                        if (!pCursor.SkipBytes(kFetchSpace)) return eProcessDataResult.notprocessed;

                        if (!cResponseDataFetch.Process(pCursor, lMSN, mGetCapability(), out lFetch, lContext))
                        {
                            lContext.TraceWarning("likely malformed fetch response");
                            return eProcessDataResult.notprocessed;
                        }
                    }

                    // fetch processing

                    var lFetchedItem = mItems[(int)lFetch.MSN - 1];

                    lFetchedItem.Update(mUIDValidity, lFetch, out var lAttributesSet, out var lKnownMessageFlagsSet);

                    if ((lAttributesSet & fFetchAttributes.flags) != 0)
                    {
                        if ((lFetch.Flags.KnownMessageFlags & fKnownMessageFlags.seen) == 0)
                        {
                            if (lFetchedItem.Unseen == null)
                            {
                                lFetchedItem.Unseen = true;
                                mUnseenUnknownCount--;
                                mUnseenCount++;
                                lUnseenUpdated = true;
                            }
                            else if (!lFetchedItem.Unseen.Value)
                            {
                                lFetchedItem.Unseen = true;
                                mUnseenCount++;
                                lUnseenUpdated = true;
                            }
                        }
                        else
                        {
                            if (lFetchedItem.Unseen == null)
                            {
                                lFetchedItem.Unseen = false;
                                mUnseenUnknownCount--;
                                lUnseenUpdated = true;
                            }
                            else if (lFetchedItem.Unseen.Value)
                            {
                                lFetchedItem.Unseen = false;
                                mUnseenCount--;
                                lUnseenUpdated = true;
                            }
                        }
                    }

                    if ((lAttributesSet & fFetchAttributes.uid) != 0 && lFetchedItem.UID != null) mUIDIndex.Add(lFetchedItem.UID, lFetchedItem);

                    ;?; // only store the modseq if the selectedmailbox has modseq enabled 


                    // events
                    //
                    if (mHasBeenSetAsSelected)
                    {
                        if (lFlagsSet != 0)
                        {
                            if ((lFlagsSet & fKnownFlags.answered) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsAnswered), lContext);
                            if ((lFlagsSet & fKnownFlags.flagged) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsFlagged), lContext);
                            if ((lFlagsSet & fKnownFlags.deleted) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsDeleted), lContext);
                            if ((lFlagsSet & fKnownFlags.seen) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsSeen), lContext);
                            if ((lFlagsSet & fKnownFlags.draft) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsDraft), lContext);

                            if ((lFlagsSet & fKnownFlags.recent) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsRecent), lContext);

                            if ((lFlagsSet & fKnownFlags.mdnsent) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsMDNSent), lContext);
                            if ((lFlagsSet & fKnownFlags.forwarded) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsForwarded), lContext);
                            if ((lFlagsSet & fKnownFlags.submitpending) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsSubmitPending), lContext);
                            if ((lFlagsSet & fKnownFlags.submitted) != 0) mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.IsSubmitted), lContext);

                            ;?; // modseq can change also

                            mEventSynchroniser.MessagePropertyChanged(mMailboxId, lFetchedItem, nameof(cMessage.Flags), lContext);
                        }

                        if (lUnseenUpdated) mEventSynchroniser.MailboxPropertyChanged(mMailboxId, nameof(iMailboxProperties.Unseen), lContext);
                    }

                    // done
                    return eProcessDataResult.observed;
                }

                public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(ProcessTextCode));

                    if (pCursor.SkipBytes(kUIDNextSpace))
                    {
                        if (pCursor.GetNZNumber(out _, out var lNumber) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("got uidnext: {0}", lNumber);

                            mUIDNext = lNumber;

                            if (mHasBeenSetAsSelected)
                            {
                                mNewUnknownUIDCount = 0;
                                ZUpdateMailboxStatus(lContext);
                            }

                            return true;
                        }

                        ;?; // consider the maintenance of the newunknown and the uidnext

                        lContext.TraceWarning("likely malformed uidnext response");
                    }


                }

                public void IncreaseCount(int pNewMessageCount, cTrace.cContext pParentContext)
                {
                    ;?; // put htis back above
                    var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(IncreaseCount), pNewMessageCount);

                    if (pNewMessageCount < mItems.Count) throw new cUnexpectedServerActionException(0, "count should only go up", lContext);

                    int lToAdd = pNewMessageCount - mItems.Count;

                    cHandleList lHandles;
                    if (mHasBeenSetAsSelected) lHandles = new cHandleList();
                    else lHandles = null;

                    for (int i = 0; i < lToAdd; i++)
                    {
                        cItem lItem = new cItem(this, mCacheSequence++);
                        mItems.Add(lItem);
                        if (mHasBeenSetAsSelected) lHandles.Add(lItem);
                    }

                    if (mHasBeenSetAsSelected) mNewUnknownUIDCount += lToAdd;
                    mUnseenUnknownCount += lToAdd;

                    ;?; // if the uidvalidity changes on a large mailbox this will be all the messages and that is probably not what is wanted
                    if (mHasBeenSetAsSelected)
                    {
                        ZUpdateMailboxStatus(lContext);
                        mEventSynchroniser.MailboxMessageDelivery(mMailboxId, lHandles, lContext);
                    }
                }








                private void ZUpdateMailboxStatus(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(ZUpdateMailboxStatus));
                    if (!mHasBeenSetAsSelected) throw new InvalidOperationException();
                    mMailboxCache.SetMailboxStatus(new cMailboxStatus(mItems.Count, mRecentCount, mUIDNext, mNewUnknownUIDCount, mUIDValidity, mUnseenCount, mUnseenUnknownCount, mHighestModSeq), lContext);
                }







                public override string ToString() => $"{nameof(cSelectedMailboxMessageCache)}({mMailboxCacheItem},{mUIDValidity})";

                private class cItem : cMessageCacheItem, IComparable<cItem>
                {
                    public bool? Unseen = null; // is this message unseen (null = don't know)

                    public cItem(cSelectedMailboxMessageCache pCache, int pCacheSequence) : base(pCache, pCacheSequence) { }

                    public int CompareTo(cItem pOther)
                    {
                        if (pOther == null) return 1;
                        return CacheSequence.CompareTo(pOther.CacheSequence);
                    }
                }
            }
        }
    }
}