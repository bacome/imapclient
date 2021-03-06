﻿using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailboxCache : iMessageCache
            {
                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly cMailboxCacheItem mMailboxCacheItem;
                private readonly uint mUIDValidity;
                private readonly bool mNoModSeq;

                private int mCacheSequence = 0;
                private readonly List<cItem> mItems;
                private readonly Dictionary<cUID, iMessageHandle> mUIDIndex = new Dictionary<cUID, iMessageHandle>();

                private int mRecentCount;

                private uint mUIDNext;
                private int mUIDNextMessageCount;
                private int mUIDNextUnknownCount;

                private int mUnseenCount;
                private int mUnseenUnknownCount;

                private ulong mHighestModSeq;
                private ulong mPendingHighestModSeq = 0;

                public cSelectedMailboxCache(cCallbackSynchroniser pSynchroniser, cMailboxCacheItem pMailboxCacheItem, uint pUIDValidity, int pMessageCount, int pRecentCount, uint pUIDNext, uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailboxCache), pMailboxCacheItem, pUIDValidity, pMessageCount, pRecentCount, pUIDNext, pHighestModSeq);

                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
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

                public cSelectedMailboxCache(cSelectedMailboxCache pOldCache, uint pUIDValidity, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailboxCache), pOldCache, pUIDValidity);

                    mSynchroniser = pOldCache.mSynchroniser;
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

                public int Count => mItems.Count;
                public iMessageHandle this[int i] => mItems[i];
                public IEnumerator<iMessageHandle> GetEnumerator() { foreach (var lItem in mItems) yield return lItem; }
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                public iMailboxHandle MailboxHandle => mMailboxCacheItem;
                public bool NoModSeq => mNoModSeq;
                public int RecentCount => mRecentCount;
                public uint UIDNext => mUIDNext;
                public int UIDNextUnknownCount => mUIDNextUnknownCount;
                public uint UIDValidity => mUIDValidity;
                public int UnseenCount => mUnseenCount;
                public int UnseenUnknownCount => mUnseenUnknownCount;
                public ulong HighestModSeq => mHighestModSeq;

                public void UpdateHighestModSeq(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(UpdateHighestModSeq));

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
                    if (mUIDIndex.TryGetValue(pUID, out var lMessageHandle)) return lMessageHandle;
                    return null;
                }

                public uint GetMSN(iMessageHandle pMessageHandle)
                {
                    // this should only be called when no msnunsafe commands are running
                    //  zero return means that the message isn't cached

                    if (!(pMessageHandle is cItem lItem)) return 0;
                    if (!ReferenceEquals(lItem.MessageCache, this)) return 0;
                    int lIndex = mItems.BinarySearch(lItem);
                    if (lIndex < 0) return 0;
                    return (uint)lIndex + 1;
                }

                public cMessageHandleList SetUnseenCount(int pMessageCount, cUIntList pMSNs, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(SetUnseenCount), pMSNs);

                    // the message count is required because messages can be delivered during the search, after the search results are calculated
                    //  it should be the message count that existed when the command was submitted
                    //   corrollary: fetches may arrive during the search that set the flags after the results were calculated
                    //    - that is why this code only changes the unseen == null entries
                    //  (I don't have to worry about expunges, as they are not allowed during a search command)

                    int lMessageCount = pMessageCount;
                    cMessageHandleList lMessageHandles = new cMessageHandleList();
                    bool lSetMailboxStatus = false;

                    foreach (var lMSN in pMSNs)
                    {
                        int lItemIndex = (int)lMSN - 1;

                        if (lItemIndex >= lMessageCount) lMessageCount = lItemIndex + 1;

                        var lItem = mItems[lItemIndex];

                        if (lItem.Unseen == null)
                        {
                            lItem.Unseen = true;
                            mUnseenCount++;
                            mUnseenUnknownCount--;
                            lSetMailboxStatus = true;
                        }

                        lMessageHandles.Add(lItem);
                    }

                    if (mUnseenUnknownCount > 0)
                    {
                        for (int i = 0; i < lMessageCount; i++)
                        {
                            var lItem = mItems[i];

                            if (lItem.Unseen == null)
                            {
                                lItem.Unseen = false;
                                mUnseenUnknownCount--;
                                lSetMailboxStatus = true;
                            }
                        }
                    }

                    if (lSetMailboxStatus) ZSetMailboxStatus(lContext);
                    
                    return lMessageHandles;
                }

                private void ZExists(int pMessageCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZExists), pMessageCount);

                    if (pMessageCount < mItems.Count) throw new cUnexpectedServerActionException(0, "count should only go up", lContext);

                    int lToAdd = pMessageCount - mItems.Count;

                    cMessageHandleList lMessageHandles = new cMessageHandleList();

                    for (int i = 0; i < lToAdd; i++)
                    {
                        cItem lItem = new cItem(this, mCacheSequence++);
                        mItems.Add(lItem);
                        lMessageHandles.Add(lItem);
                    }

                    mUIDNextUnknownCount += lToAdd;
                    mUnseenUnknownCount += lToAdd;

                    mSynchroniser.InvokeMailboxMessageDelivery(mMailboxCacheItem, lMessageHandles, lContext);
                    ZSetMailboxStatus(lContext);
                }

                private void ZRecent(int pRecentCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZRecent), pRecentCount);
                    mRecentCount = pRecentCount;
                    ZSetMailboxStatus(lContext);
                }

                private void ZExpunge(int pMSN, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZExpunge), pMSN);
                    
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

                    mSynchroniser.InvokeMessagePropertyChanged(lExpungedItem, nameof(cMessage.Expunged), lContext);
                    ZSetMailboxStatus(lContext);
                }

                private void ZFetch(cResponseDataFetch pFetch, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZFetch));

                    var lFetchedItem = mItems[(int)pFetch.MSN - 1];

                    lFetchedItem.Update(pFetch, out var lAttributesSet, out var lPropertiesChanged);

                    bool lSetMailboxStatus = false;

                    if ((lAttributesSet & fMessageCacheAttributes.flags) != 0)
                    {
                        if (pFetch.Flags.Contains(kMessageFlag.Seen))
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
                        else
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
                    }

                    if ((lAttributesSet & fMessageCacheAttributes.uid) != 0 && lFetchedItem.UID != null)
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

                    mSynchroniser.InvokeMessagePropertiesChanged(lFetchedItem, lPropertiesChanged, lContext);
                    if (lSetMailboxStatus) ZSetMailboxStatus(lContext);
                }

                private void ZUIDNext(uint pUIDNext, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZUIDNext), pUIDNext);

                    mUIDNext = pUIDNext;
                    mUIDNextMessageCount = mItems.Count;
                    mUIDNextUnknownCount = 0;

                    ZSetMailboxStatus(lContext);
                }

                private void ZHighestModSeq(uint pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZHighestModSeq), pHighestModSeq);
                    mPendingHighestModSeq = pHighestModSeq;
                }

                private void ZSetMailboxStatus(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZSetMailboxStatus));
                    mMailboxCacheItem.SetMailboxStatus(new cMailboxStatus(mItems.Count, mRecentCount, mUIDNext, mUIDNextUnknownCount, mUIDValidity, mUnseenCount, mUnseenUnknownCount, mHighestModSeq), lContext);
                }

                public override string ToString() => $"{nameof(cSelectedMailboxCache)}({mMailboxCacheItem},{mUIDValidity})";
                
                private class cItem : cMessageCacheItem, IComparable<cItem> // icomparable is implemented for binary search => no need for iequatable
                {
                    public bool? Unseen = null; // is this message unseen (null = don't know)

                    public cItem(cSelectedMailboxCache pCache, int pCacheSequence) : base(pCache, pCacheSequence) { }

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