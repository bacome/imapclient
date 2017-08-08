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
                private ulong mPendingHighestModSeq = 0;

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

                    if (mUIDNext == 0)
                    {
                        mUIDNextMessageCount = 0;
                        mUIDNextUnknownCount = pMessageCount;
                    }
                    else
                    {
                        mUIDNextMessageCount = pMessageCount;
                        mUIDNextUnknownCount = 0;
                    }

                    mUnseenCount = 0;
                    mUnseenUnknownCount = pMessageCount;

                    mHighestModSeq = pHighestModSeq;

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

                    ZSetMailboxStatus(lContext);
                }

                public iMailboxHandle MailboxHandle => mMailboxCacheItem;
                public bool NoModSeq => mNoModSeq;
                public int MessageCount => mItems.Count;
                public int RecentCount => mRecentCount;
                public uint UIDNext => mUIDNext;
                public int UIDNextUnknownCount => mUIDNextUnknownCount;
                public uint UIDValidity => mUIDValidity;
                public int UnseenCount => mUnseenCount;
                public int UnseenUnknownCount => mUnseenUnknownCount;
                public ulong HighestModSeq => mHighestModSeq;

                public void UpdateHighestModSeq(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(UpdateHighestModSeq));

                    if (mNoModSeq) throw new InvalidOperationException();

                    if (mPendingHighestModSeq > mHighestModSeq)
                    {
                        mHighestModSeq = mPendingHighestModSeq;
                        ZSetMailboxStatus(lContext);
                    }

                    mPendingHighestModSeq = 0;
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

                public cMessageHandleList SetUnseen(cUIntList pMSNs, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(SetUnseen), pMSNs);

                    cMessageHandleList lHandles = new cMessageHandleList();

                    foreach (var lMSN in pMSNs)
                    {
                        var lItem = mItems[(int)lMSN - 1];
                        lItem.Unseen = true;
                        lHandles.Add(lItem);
                    }

                    if (mUnseenUnknownCount > 0)
                    {
                        foreach (var lItem in mItems) if (lItem.Unseen == null) lItem.Unseen = false;
                        mUnseenUnknownCount = 0;
                        mUnseenCount = pMSNs.Count;
                        ZSetMailboxStatus(lContext);
                    }

                    return lHandles;
                }

                private void ZExists(int pMessageCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ZExists), pMessageCount);

                    if (pMessageCount < mItems.Count) throw new cUnexpectedServerActionException(0, "count should only go up", lContext);

                    int lToAdd = pMessageCount - mItems.Count;

                    cMessageHandleList lHandles = new cMessageHandleList();

                    for (int i = 0; i < lToAdd; i++)
                    {
                        cItem lItem = new cItem(this, mCacheSequence++);
                        mItems.Add(lItem);
                        lHandles.Add(lItem);
                    }

                    mUIDNextUnknownCount += lToAdd;
                    mUnseenUnknownCount += lToAdd;

                    mEventSynchroniser.FireMailboxMessageDelivery(mMailboxCacheItem, lHandles, lContext);
                    ZSetMailboxStatus(lContext);
                }

                private void ZRecent(int pRecentCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ZRecent), pRecentCount);
                    mRecentCount = pRecentCount;
                    ZSetMailboxStatus(lContext);
                }

                private void ZExpunge(int pMSN, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ZExpunge), pMSN);
                    
                    int lIndex = pMSN - 1;
                    var lExpungedItem = mItems[lIndex];
                    mItems.RemoveAt(lIndex);

                    lExpungedItem.SetExpunged();

                    if (lExpungedItem.Unseen == null) mUnseenUnknownCount--;
                    else if (lExpungedItem.Unseen == true) mUnseenCount--;

                    if (pMSN > mUIDNextMessageCount)
                    {
                        if (lExpungedItem.UID == null) mUIDNextUnknownCount--;
                    }
                    else mUIDNextMessageCount--;

                    mEventSynchroniser.FireMessagePropertiesChanged(lExpungedItem, fMessageProperties.isexpunged, lContext);
                    ZSetMailboxStatus(lContext);
                }

                private void ZFetch(cResponseDataFetch pFetch, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ZFetch));

                    var lFetchedItem = mItems[(int)pFetch.MSN - 1];

                    lFetchedItem.Update(mUIDValidity, mNoModSeq, pFetch, out var lAttributesSet, out var lKnownMessageFlagsSet, out var lProperties);

                    bool lSetMailboxStatus = false;

                    if ((lAttributesSet & fFetchAttributes.flags) != 0)
                    {
                        if ((pFetch.Flags.KnownMessageFlags & fKnownMessageFlags.seen) == 0)
                        {
                            if (lFetchedItem.Unseen == null)
                            {
                                lFetchedItem.Unseen = true;
                                mUnseenUnknownCount--;
                                mUnseenCount++;
                                lSetMailboxStatus = true;
                            }
                            else if (!lFetchedItem.Unseen.Value)
                            {
                                lFetchedItem.Unseen = true;
                                mUnseenCount++;
                                lSetMailboxStatus = true;
                            }
                        }
                        else
                        {
                            if (lFetchedItem.Unseen == null)
                            {
                                lFetchedItem.Unseen = false;
                                mUnseenUnknownCount--;
                                lSetMailboxStatus = true;
                            }
                            else if (lFetchedItem.Unseen.Value)
                            {
                                lFetchedItem.Unseen = false;
                                mUnseenCount--;
                                lSetMailboxStatus = true;
                            }
                        }
                    }

                    if ((lAttributesSet & fFetchAttributes.uid) != 0 && lFetchedItem.UID != null)
                    {
                        mUIDIndex.Add(lFetchedItem.UID, lFetchedItem);

                        if (pFetch.MSN > mUIDNextMessageCount)
                        {
                            mUIDNextUnknownCount--;
                            if (lFetchedItem.UID.UID + 1 > mUIDNext) mUIDNext = lFetchedItem.UID.UID + 1;
                            lSetMailboxStatus = true;
                        }
                    }

                    if (lFetchedItem.ModSeq > mPendingHighestModSeq) mPendingHighestModSeq = lFetchedItem.ModSeq.Value;

                    mEventSynchroniser.FireMessagePropertiesChanged(lFetchedItem, lProperties, lContext);
                    if (lSetMailboxStatus) ZSetMailboxStatus(lContext);
                }

                private void ZUIDNext(uint pUIDNext, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ZUIDNext), pUIDNext);

                    mUIDNext = pUIDNext;
                    mUIDNextMessageCount = mItems.Count;
                    mUIDNextUnknownCount = 0;

                    ZSetMailboxStatus(lContext);
                }

                private void ZHighestModSeq(uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ZHighestModSeq), pHighestModSeq);
                    mPendingHighestModSeq = pHighestModSeq;
                }

                private void ZSetMailboxStatus(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxMessageCache), nameof(ZSetMailboxStatus));
                    mMailboxCacheItem.SetMailboxStatus(new cMailboxStatus(mItems.Count, mRecentCount, mUIDNext, mUIDNextUnknownCount, mUIDValidity, mUnseenCount, mUnseenUnknownCount, mHighestModSeq), lContext);
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