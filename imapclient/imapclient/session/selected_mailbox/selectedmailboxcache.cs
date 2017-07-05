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
                private class cCache : iMessageCache
                {
                    private static readonly cBytes kExpunge = new cBytes("EXPUNGE");
                    private static readonly cBytes kFetchSpace = new cBytes("FETCH ");

                    private readonly cMailboxId mMailboxId;
                    private readonly uint? mUIDValidity;
                    private readonly cEventSynchroniser mEventSynchroniser;
                    private dGetCapability mGetCapability;
                    private bool mHasBeenSetAsSelected;
                    private bool mValid = true;
                    private int mCacheSequence = 0;
                    private List<cItem> mItems = new List<cItem>();
                    private SortedDictionary<cUID, iMessageHandle> mUIDIndex = new SortedDictionary<cUID, iMessageHandle>();
                    private int mUnseenTrue = 0; // number of messages with unseen = true
                    private int mUnseenNull = 0; // number of message with unseen = null 

                    public cCache(cMailboxId pMailboxId, uint? pUIDValidity, cEventSynchroniser pEventSynchroniser, dGetCapability pGetCapability, bool pHasBeenSetAsSelected)
                    {
                        mMailboxId = pMailboxId;
                        mUIDValidity = pUIDValidity;
                        mEventSynchroniser = pEventSynchroniser;
                        mGetCapability = pGetCapability;
                        mHasBeenSetAsSelected = pHasBeenSetAsSelected;
                    }

                    public uint? UIDValidity => mUIDValidity;

                    public void SetAsSelected(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCache), nameof(SetAsSelected));
                        if (mHasBeenSetAsSelected) throw new InvalidOperationException();
                        mHasBeenSetAsSelected = true;
                    }

                    public bool Valid => mValid;

                    public void Invalidate(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCache), nameof(Invalidate));
                        mValid = false;
                    }

                    public int Count => mItems.Count;

                    public int UnseenTrue => mUnseenTrue;
                    public int UnseenNull => mUnseenNull;

                    public void IncreaseCount(int pNewCount, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCache), nameof(IncreaseCount), pNewCount);
                        if (pNewCount < mItems.Count) throw new cUnexpectedServerActionException(0, "count should only go up", lContext);

                        int lToAdd = pNewCount - mItems.Count;

                        cHandleList lHandles;
                        if (mHasBeenSetAsSelected) lHandles = new cHandleList();
                        else lHandles = null;

                        for (int i = 0; i < lToAdd; i++)
                        {
                            cItem lItem = new cItem(this, mCacheSequence++);
                            mItems.Add(lItem);
                            if (mHasBeenSetAsSelected) lHandles.Add(lItem);
                        }

                        mUnseenNull += lToAdd;

                        if (mHasBeenSetAsSelected) mEventSynchroniser.MailboxMessageDelivery(mMailboxId, lHandles, lContext);
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
                        if (!ReferenceEquals(lItem.Cache, this)) return 0;
                        int lIndex = mItems.BinarySearch(lItem);
                        if (lIndex < 0) return 0;
                        return (uint)lIndex + 1;
                    }

                    public int SetUnseenCount { get; set; } // marker that indicates the point up to which that we can set unseen = false when storing the results of search unseen

                    public int SetUnseen(cUIntList pMSNs, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCache), nameof(SetUnseen), pMSNs);

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
                                mUnseenNull--;
                                mUnseenTrue++;
                                lUnseenUpdated = true;
                            }
                        }

                        for (int i = 0; i <= lMaxIndex; i++)
                        {
                            var lItem = mItems[i];

                            if (lItem.Unseen == null)
                            {
                                lItem.Unseen = false;
                                mUnseenNull--;
                                lUnseenUpdated = true;
                            }
                        }

                        if (lUnseenUpdated) mEventSynchroniser.MailboxPropertyChanged(mMailboxId, nameof(iMailboxProperties.Unseen), lContext);

                        int lUnseen = 0;
                        for (int i = 0; i < SetUnseenCount; i++) if (mItems[i].Unseen == true) lUnseen++;
                        return lUnseen;
                    }

                    public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCache), nameof(ProcessData));

                        bool lUnseenUpdated = false;
                        cResponseDataFetch lFetch;

                        if (pCursor.Parsed)
                        {
                            lFetch = pCursor.ParsedAs as cResponseDataFetch;
                            if (lFetch == null) return eProcessDataResult.notprocessed;
                        }
                        else
                        {
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
                                    mUnseenNull--;
                                    lUnseenUpdated = true;
                                }
                                else if (lExpungedItem.Unseen.Value)
                                {
                                    mUnseenTrue--;
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

                        lFetchedItem.Update(mUIDValidity, lFetch, out var lAttributesSet, out var lFlagsSet);

                        if ((lAttributesSet & fFetchAttributes.flags) != 0)
                        {
                            if ((lFetch.Flags.KnownFlags & fKnownFlags.seen) == 0)
                            {
                                if (lFetchedItem.Unseen == null)
                                {
                                    lFetchedItem.Unseen = true;
                                    mUnseenNull--;
                                    mUnseenTrue++;
                                    lUnseenUpdated = true;
                                }
                                else if (!lFetchedItem.Unseen.Value)
                                {
                                    lFetchedItem.Unseen = true;
                                    mUnseenTrue++;
                                    lUnseenUpdated = true;
                                }
                            }
                            else
                            {
                                if (lFetchedItem.Unseen == null)
                                {
                                    lFetchedItem.Unseen = false;
                                    mUnseenNull--;
                                    lUnseenUpdated = true;
                                }
                                else if (lFetchedItem.Unseen.Value)
                                {
                                    lFetchedItem.Unseen = false;
                                    mUnseenTrue--;
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

                    public override string ToString() => $"{nameof(cCache)}({mMailboxId},{mUIDValidity},{mValid})";

                    private class cItem : cMessageCacheItem, IComparable<cItem>
                    {
                        public bool? Unseen = null; // is this message unseen (null = don't know)

                        public cItem(cCache pCache, int pCacheSequence) : base(pCache, pCacheSequence) { }

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