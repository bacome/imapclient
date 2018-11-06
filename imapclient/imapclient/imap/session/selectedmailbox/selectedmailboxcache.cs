using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cSelectedMailboxCache : iMessageCache
            {
                private readonly cPersistentCache mPersistentCache;
                private readonly cIMAPCallbackSynchroniser mSynchroniser;
                private readonly bool mUTF8Enabled;
                private readonly cMailboxCacheItem mMailboxCacheItem;
                private readonly uint mUIDValidity;
                private readonly bool mUIDNotSticky;
                private readonly cMailboxUID mMailboxUID;
                private readonly bool mNoModSeq;

                private int mCacheSequence = 0;
                private readonly List<cItem> mItems;
                private readonly ConcurrentDictionary<cUID, iMessageHandle> mUIDIndex = new ConcurrentDictionary<cUID, iMessageHandle>();

                private int mRecentCount;

                private uint mUIDNextComponent;
                private cUID mUIDNext;
                private int mUIDNextMessageCount;
                private int mUIDNextUnknownCount;

                private int mUnseenCount;
                private int mUnseenUnknownCount;

                private ulong mHighestModSeq;
                private ulong mPendingHighestModSeq = 0;

                private bool mSelected;
                private bool mSynchronised;
                private bool mCallSetHighestModSeq;
                private bool mIsInvalid = false;

                public cSelectedMailboxCache(cPersistentCache pPersistentCache, cIMAPCallbackSynchroniser pSynchroniser, bool pUTF8Enabled, cMailboxCacheItem pMailboxCacheItem, uint pUIDValidity, bool pUIDNotSticky, int pMessageCount, int pRecentCount, uint pUIDNextComponent, ulong pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailboxCache), pMailboxCacheItem, pUIDValidity, pUIDNotSticky, pMessageCount, pRecentCount, pUIDNextComponent, pHighestModSeq);

                    mPersistentCache = pPersistentCache ?? throw new ArgumentNullException(nameof(pPersistentCache));
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mUTF8Enabled = pUTF8Enabled;
                    mMailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mUIDValidity = pUIDValidity;
                    mUIDNotSticky = pUIDNotSticky;
                    
                    if (pUIDValidity == 0) mMailboxUID = null;
                    else mMailboxUID = new cMailboxUID(pMailboxCacheItem.MailboxId, pUIDValidity, pUIDNotSticky);

                    mNoModSeq = pHighestModSeq == 0;

                    mItems = new List<cItem>(pMessageCount);
                    for (int i = 0; i < pMessageCount; i++) mItems.Add(new cItem(this, mCacheSequence++));

                    mRecentCount = pRecentCount;

                    mUIDNextComponent = pUIDNextComponent;

                    if (mUIDNextComponent == 0)
                    {
                        mUIDNextMessageCount = 0;
                        mUIDNextUnknownCount = pMessageCount;
                    }
                    else
                    {
                        mUIDNextMessageCount = pMessageCount;
                        mUIDNextUnknownCount = 0;
                    }

                    ZSetUIDNext();

                    mUnseenCount = 0;
                    mUnseenUnknownCount = pMessageCount;

                    mHighestModSeq = pHighestModSeq;

                    mSelected = false;
                    mSynchronised = false;
                    mCallSetHighestModSeq = false;
                }

                public cSelectedMailboxCache(cSelectedMailboxCache pOldCache, uint pUIDValidity, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cSelectedMailboxCache), pOldCache, pUIDValidity);

                    // this constructor is for a uidvalidity change

                    if (pOldCache == null) throw new ArgumentNullException(nameof(pOldCache));
                    if (!pOldCache.mSelected) throw new cInternalErrorException(lContext);

                    mPersistentCache = pOldCache.mPersistentCache;
                    mSynchroniser = pOldCache.mSynchroniser;
                    mUTF8Enabled = pOldCache.mUTF8Enabled;
                    mMailboxCacheItem = pOldCache.mMailboxCacheItem;
                    mUIDValidity = pUIDValidity;
                    mUIDNotSticky = pOldCache.mUIDNotSticky;

                    if (pUIDValidity == 0) mMailboxUID = null;
                    else mMailboxUID = new cMailboxUID(pOldCache.mMailboxCacheItem.MailboxId, pUIDValidity, pOldCache.mUIDNotSticky);

                    mNoModSeq = pOldCache.mNoModSeq;

                    int lMessageCount = pOldCache.mItems.Count;

                    mItems = new List<cItem>(lMessageCount);
                    for (int i = 0; i < lMessageCount; i++) mItems.Add(new cItem(this, mCacheSequence++));

                    mRecentCount = pOldCache.mRecentCount;

                    mUIDNextComponent = 0;
                    mUIDNextMessageCount = 0;
                    mUIDNextUnknownCount = lMessageCount;
                    ZSetUIDNext();

                    mUnseenCount = 0;
                    mUnseenUnknownCount = lMessageCount;

                    mHighestModSeq = 0;

                    mSelected = true;
                    mSynchronised = true;
                    mCallSetHighestModSeq = !mUIDNotSticky && !mNoModSeq; 

                    ZSetMailboxStatus(lContext);
                }

                public void SetSelected(cFetchableFlags pMessageFlags, bool pForUpdate, cPermanentFlags pPermanentFlags, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(SetEnabled));

                    if (mSelected) throw new cInternalErrorException(lContext);
                    mSelected = true;

                    ZSetMailboxStatus(lContext);
                    mMailboxCacheItem.SetSelectedProperties(pMessageFlags, pForUpdate, pPermanentFlags, mUIDNotSticky, lContext);
                }

                public void SetSynchronised(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(SetSynchronised));

                    if (mSynchronised) throw new cInternalErrorException(lContext);
                    mSynchronised = true;

                    mCallSetHighestModSeq = !mUIDNotSticky && !mNoModSeq;

                    if (mCallSetHighestModSeq) mPersistentCache.SetHighestModSeq(mMailboxUID, mHighestModSeq, lContext);
                }

                public void SetInvalid(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(SetInvalid));
                    if (!mSelected || mIsInvalid) throw new cInternalErrorException(lContext);
                    mIsInvalid = true;
                    mPersistentCache.MessageCacheInvalidated(this, lContext);
                }

                public int Count => mItems.Count;
                public iMessageHandle this[int i] => mItems[i];
                public IEnumerator<iMessageHandle> GetEnumerator() { foreach (var lItem in mItems) yield return lItem; }
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                public iMailboxHandle MailboxHandle => mMailboxCacheItem;
                public bool NoModSeq => mNoModSeq;
                public int RecentCount => mRecentCount;
                public uint UIDValidity => mUIDValidity;
                public bool UIDNotSticky => mUIDNotSticky;

                public cUID UIDNext => mUIDNext;

                public int UIDNextUnknownCount => mUIDNextUnknownCount;
                public int UnseenCount => mUnseenCount;
                public int UnseenUnknownCount => mUnseenUnknownCount;
                public ulong HighestModSeq => mHighestModSeq;
                public bool IsInvalid => mIsInvalid;

                public bool HasPendingHighestModSeq()
                {
                    if (mNoModSeq) throw new InvalidOperationException();
                    return mPendingHighestModSeq > mHighestModSeq;
                }

                public void UpdateHighestModSeq(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(UpdateHighestModSeq));

                    if (mNoModSeq) throw new cInternalErrorException(lContext);

                    if (mPendingHighestModSeq > mHighestModSeq)
                    {
                        mHighestModSeq = mPendingHighestModSeq;
                        if (mCallSetHighestModSeq) mPersistentCache.SetHighestModSeq(mMailboxUID, mHighestModSeq, lContext);
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

                public cMessageHandleList SetUnseenCount(int pMessageCount, IEnumerable<uint> pMSNs, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(SetUnseenCount));

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

                public List<cMessageUID> GetMessageUIDsWithDeletedFlag(cTrace.cContext pParentContext)
                {
                    ;?; // hmmm ... the problem here is, probably what is important is what ones I"VE been told about
                    // and at the moment I don't have a way of knowing as I will pick up the flag changes told to other clients sharing the cache
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(GetMessageUIDsWithDeletedFlag));
                    return new List<cMessageUID>(from lItem in mItems where lItem.MessageUID != null && lItem.Flags != null && lItem.Flags.Contains(kMessageFlag.Deleted) select lItem.MessageUID);
                }

                private void ZExists(int pMessageCount, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZExists), pMessageCount);

                    if (pMessageCount < mItems.Count) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.CountShouldOnlyGoUp, 0, lContext);

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
                        if (lExpungedItem.MessageUID == null) mUIDNextUnknownCount--;
                    }
                    else mUIDNextMessageCount--;

                    if (lExpungedItem.MessageUID != null) mUIDIndex.TryRemove(lExpungedItem.MessageUID.UID, out _);

                    mPersistentCache.MessageExpunged(lExpungedItem, lContext);

                    mSynchroniser.InvokeMessagePropertyChanged(lExpungedItem, nameof(cIMAPMessage.Expunged), lContext);
                    ZSetMailboxStatus(lContext);
                }

                private void ZFetch(cResponseDataFetch pFetch, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZFetch), pFetch);

                    var lFetchedItem = mItems[(int)pFetch.MSN - 1];

                    lFetchedItem.Update(pFetch, out var lUIDWasSet, out var lMessagePropertiesChanged, lContext);

                    bool lSetMailboxStatus = false;

                    if (lUIDWasSet)
                    {
                        mUIDIndex[lFetchedItem.MessageUID.UID] = lFetchedItem;

                        if (pFetch.MSN > mUIDNextMessageCount)
                        {
                            mUIDNextUnknownCount--;

                            if (lFetchedItem.MessageUID.UID.UID + 1 > mUIDNextComponent)
                            {
                                mUIDNextComponent = lFetchedItem.MessageUID.UID.UID + 1;
                                ZSetUIDNext();
                            }

                            lSetMailboxStatus = true;
                        }
                    }

                    if (lFetchedItem.Seen == true)
                    {
                        if (lFetchedItem.Unseen == null)
                        {
                            lFetchedItem.Unseen = false;
                            mUnseenUnknownCount--;
                            lSetMailboxStatus = true;
                        }
                        else if (lFetchedItem.Unseen == true)
                        {
                            lFetchedItem.Unseen = false;
                            mUnseenCount--;
                            lSetMailboxStatus = true;
                        }
                    }
                    else if (lFetchedItem.Seen == false)
                    {
                        if (lFetchedItem.Unseen == null)
                        {
                            lFetchedItem.Unseen = true;
                            mUnseenUnknownCount--;
                            mUnseenCount++;
                            lSetMailboxStatus = true;
                        }
                        else if (lFetchedItem.Unseen == false)
                        {
                            lFetchedItem.Unseen = true;
                            mUnseenCount++;
                            lSetMailboxStatus = true;
                        }
                    }

                    ;?; // this also has to use the last told value, not the cached value

                    if (mSelected) 
                    {
                        if (lFetchedItem.ModSeqFlags != null && lFetchedItem.ModSeqFlags.ModSeq > mPendingHighestModSeq) mPendingHighestModSeq = lFetchedItem.ModSeqFlags.ModSeq.Value;
                        mSynchroniser.InvokeMessagePropertiesChanged(lFetchedItem, lModSeqFlagPropertiesChanged, lContext);
                        if (lSetMailboxStatus) ZSetMailboxStatus(lContext);
                    }
                }

                private void ZUIDNext(uint pUIDNextComponent, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZUIDNext), pUIDNextComponent);

                    mUIDNextComponent = pUIDNextComponent;
                    mUIDNextMessageCount = mItems.Count;
                    mUIDNextUnknownCount = 0;
                    ZSetUIDNext();

                    ZSetMailboxStatus(lContext);
                }

                private void ZSetUIDNext()
                {
                    if (mUIDValidity == 0 || mUIDNextComponent == 0) mUIDNext = null;
                    else mUIDNext = new cUID(mUIDValidity, mUIDNextComponent);
                }

                private void ZHighestModSeq(ulong pHighestModSeq, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZHighestModSeq), pHighestModSeq);
                    mPendingHighestModSeq = pHighestModSeq;
                }

                private void ZSetMailboxStatus(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectedMailboxCache), nameof(ZSetMailboxStatus));
                    mMailboxCacheItem.SetMailboxStatus(new cMailboxStatus(mItems.Count, mRecentCount, mUIDValidity, mUIDNextComponent, mUIDNextUnknownCount, mUnseenCount, mUnseenUnknownCount, mHighestModSeq), lContext);
                }

                private void ZNoModSeqFlagUpdate(cTrace.cContext pParentContext) => mPersistentCache.NoModSeqFlagUpdate(mMailboxUID, pParentContext);

                public override string ToString() => $"{nameof(cSelectedMailboxCache)}({mMailboxCacheItem},{mUIDValidity})";
            }
        }
    }
}