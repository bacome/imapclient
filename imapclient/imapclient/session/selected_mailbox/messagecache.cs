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
                private class cMessageCache
                {
                    private static readonly cBytes kExpunge = new cBytes("EXPUNGE");
                    private static readonly cBytes kFetchSpace = new cBytes("FETCH ");

                    private readonly cMailboxId mMailboxId;
                    private readonly uint mUIDValidity;
                    private bool mHasBeenSetAsSelected;
                    private readonly cEventSynchroniser mEventSynchroniser;
                    private readonly cMailboxCache mMailboxCache;
                    private int mCacheSequence = 0;
                    private List<cItem> mItems = new List<cItem>();
                    private SortedDictionary<cUID, iMessageHandle> mUIDIndex = new SortedDictionary<cUID, iMessageHandle>();
                    private int mRecentCount = 0;
                    private uint mUIDNext = 0;
                    private int mNewUnknownUIDCount = 0;
                    private int mUnseenCount = 0; // number of messages with unseen = true
                    private int mUnseenUnknownCount = 0; // number of message with unseen = null 
                    private ulong mHighestModSeq = 0; // still to do everything for this ...

                    public cMessageCache(cMailboxId pMailboxId, uint pUIDValidity, bool pHasBeenSetAsSelected, cEventSynchroniser pEventSynchroniser, cMailboxCache pMailboxCache)
                    {
                        mMailboxId = pMailboxId ?? throw new ArgumentNullException(nameof(pMailboxId));
                        mUIDValidity = pUIDValidity;
                        mHasBeenSetAsSelected = pHasBeenSetAsSelected;
                        mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                        mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    }

                    public void SetAsSelected(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(SetAsSelected));
                        if (mHasBeenSetAsSelected) throw new InvalidOperationException();
                        mHasBeenSetAsSelected = true;
                        ZUpdateMailboxStatus(lContext);
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

                    public int MessageCount => mItems.Count;

                    public int SetUnseenCount { get; set; } // marker that indicates the point up to which that we can set unseen = false when storing the results of search unseen

                    public void SetUnseen(cUIntList pMSNs, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(SetUnseen), pMSNs);

                        int lMaxIndex = SetUnseenCount - 1;
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

                        if (lUnseenUpdated) ZUpdateMailboxStatus(lContext);
                    }

                    public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(ProcessData));

                        bool lUnseenUpdated = false;
                        cResponseDataFetch lFetch;

                        if (pCursor.Parsed)
                        {
                            lFetch = pCursor.ParsedAs as cResponseDataFetch;
                            if (lFetch == null) return eProcessDataResult.notprocessed;
                        }
                        else
                        {
                            if (pCursor.GetNumber(out _, out var lNumber) && pCursor.SkipByte(cASCII.SPACE))
                            {
                                if (pCursor.SkipBytes(kExists))
                                {
                                    if (pCursor.Position.AtEnd)
                                    {
                                        int lNewCount = (int)lNumber;

                                        if (lNewCount < mItems.Count) throw new cUnexpectedServerActionException(0, "count should only go up", lContext);

                                        int lToAdd = lNewCount - mItems.Count;

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

                                        if (mHasBeenSetAsSelected)
                                        {
                                            ZUpdateMailboxStatus(lContext);
                                            mEventSynchroniser.MailboxMessageDelivery(mMailboxId, lHandles, lContext);
                                        }

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

                                ;?; // the stuff below goes here UP TO HERE!!!!
                            }




                            if (!pCursor.GetNZNumber(out _, out var lMSN) || !pCursor.SkipByte(cASCII.SPACE)) return eProcessDataResult.notprocessed;

                            if (pCursor.SkipBytes(kExpunge))
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










                    private void ZIncreaseCount(int pNewCount, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(IncreaseCount), pNewCount);
                    }



                    private void ZUpdateMailboxStatus(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cMessageCache), nameof(ZUpdateMailboxStatus));
                        if (!mHasBeenSetAsSelected) throw new InvalidOperationException();
                        mMailboxCache.SetMailboxStatus(new cMailboxStatus(mItems.Count, mRecentCount, mUIDNext, mNewUnknownUIDCount, mUIDValidity, mUnseenCount, mUnseenUnknownCount, mHighestModSeq), lContext);
                    }







                    public override string ToString() => $"{nameof(cCache)}({mMailboxId},{mUIDValidity},{mValid})";

                    private class cItem : cMessageCacheItem, IComparable<cItem>
                    {
                        public bool? Unseen = null; // is this message unseen (null = don't know)

                        public cItem(cMessageCache pCache, int pCacheSequence) : base(pCache, pCacheSequence) { }

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
}