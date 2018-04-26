﻿using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCache
    {
        private readonly object mLock = new object();

        protected readonly string mInstanceName;
        protected readonly cTrace.cContext mRootContext;
        private readonly cBatchSizer mWriteSizer;
        public readonly bool Temporary;

        private Dictionary<cSectionCachePersistentKey, cItem> mPersistentKeyItems;
        private Dictionary<cSectionCacheNonPersistentKey, cItem> mNonPersistentKeyItems;

        private int mOpenAccessorCount = 0;

        protected cSectionCache(string pInstanceName, cBatchSizerConfiguration pWriteConfiguration, bool pTemporary)
        {
            mInstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));

            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
            mRootContext.TraceInformation("cSectionCache by bacome version {0}, release date {1}", cMailClient.Version, cMailClient.ReleaseDate);

            var lContext = mRootContext.NewObject(nameof(cSectionCache), pWriteConfiguration, pTemporary);

            mWriteSizer = new cBatchSizer(pWriteConfiguration);
            Temporary = pTemporary;

            mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cItem>();
            mNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cItem>();
        }

        // asks the cache if it has an item for the key
        //  WILL be called inside the lock
        //  DO NOT call directly (use the other TryGetExistingItem)
        //
        protected virtual bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cItem rItem)
        {
            throw new NotImplementedException();
        }

        // asks the cache to create a new item
        //  the item will either be deleted during creation by internal code
        //   i.e.
        //    if the retrieval fails
        //    if the item is a duplicate by the time the retrieval finishes
        //    if the item doesn't have a pk and the npk is no longer valid
        //  OR itemadded will be called
        //
        protected abstract cItem GetNewItem();

        // lets the cache know that a new item has been written
        //  allows the cache to increase the number of files in use and the total size of the cache
        //  NOTE that when this is called the item being added could be either still open or be closed
        //
        protected virtual void ItemAdded(cItem pItem) { }

        // lets the cache know that a cached item has been deleted
        //  allows the cache to decrease the number of files in use and the total size of the cache
        //
        protected virtual void ItemDeleted(cItem pItem) { }

        // lets the cache know that a previously open item has been closed
        //  if the cache is over budget, then it might be a good time to allow a cache trim to run
        //
        protected virtual void ItemClosed() { }

        // for use in cache trimming
        //
        //  NOTE that the following should be done on a separate thread, single threaded
        //
        //  if the cache is over budget
        //   if the cache is a permanent one, then there will be two lists to delete from
        //    the full list of items in the cache from the backing store and
        //    the list of items that the class knows about (which should be a subset of the full list)
        //   if the cache is a temporary one, then there will be one list - the list that the class knows about
        //
        //   use GetAllItems to get a snapshot of the items that the class knows about
        //    note that items returned are live - so their states can change at any time, that is why the change-sequence exists
        //     from the items you should build a tuple with the things that you want to sort on later, but the first property you get and store should be the change-sequence
        //  
        //   if the cache is temporary, sort by the ones you most want to delete and use TryDelete(change-sequence) on them [reducing the size of the cache only if the delete succeeds]
        //   if the cache is permanent, sort by the items you most want to delete, if there is an item use TryDelete(change-sequence) else use 
        //    GetExistingItem(pk) and then TryDelete(0)
        //
        //
        protected List<cItem> GetAllItems()
        {
            var lItems = new List<cItem>();

            lock (mLock)
            {
                foreach (var lPair in mPersistentKeyItems) lItems.Add(lPair.Value);
                foreach (var lPair in mNonPersistentKeyItems) lItems.Add(lPair.Value);
            }

            return lItems;
        }

        // for use in cache trimming
        //
        protected cItem GetExistingItem(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetExistingItem), pKey);

            lock (mLock)
            {
                if (ZZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem;
            }

            return null;
        }

        // called by the cIMAPClient
        internal cAccessor GetAccessor(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetAccessor));

            lock (mLock)
            {
                mOpenAccessorCount++;
                return new cAccessor(this, lContext);
            }
        }

        // called by cacheitem when the item is closed 
        private bool IsClosed
        {
            get
            {
                lock (mLock)
                {
                    return mOpenAccessorCount == 0;
                }
            }
        }

        // called by accessor
        private bool ZTryGetReader(cSectionCachePersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetReader), pKey);

            lock (mLock)
            {
                if (ZZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        // called by accessor
        private bool ZTryGetReader(cSectionCacheNonPersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetReader), pKey);

            lock (mLock)
            {
                if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        // called by accessor
        private cItem.cReaderWriter ZGetReaderWriter(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetReaderWriter), pKey);

            var lItem = GetNewItem();
            if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
            return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
        }

        // called by accessor
        private cItem.cReaderWriter ZGetReaderWriter(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetReaderWriter), pKey);

            var lItem = GetNewItem();
            if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
            return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
        }

        // called by cacheitem when the open count goes to zero
        private bool ZTryAssignPersistentKey(cSectionCachePersistentKey pKey, cItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryAssignPersistentKey), pKey, pItem);

            lock (mLock)
            {
                return ZZTryAssignPersistentKey(pKey, pItem, lContext);
            }
        }

        // called by accessor when it is disposed
        private void ZDecrementOpenAccessorCount(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZDecrementOpenAccessorCount));

            lock (mLock)
            {
                mOpenAccessorCount--;

                if (mOpenAccessorCount == 0 && Temporary)
                {
                    var lPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cItem>();
                    foreach (var lPair in mPersistentKeyItems) if (lPair.Value.TryDelete(-1, lContext) == eSectionCacheItemDeleteResult.notdeleted) lPersistentKeyItems.Add(lPair.Key, lPair.Value);
                    mPersistentKeyItems = lPersistentKeyItems;

                    var lNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cItem>();
                    foreach (var lPair in mNonPersistentKeyItems) if (lPair.Value.TryDelete(-1, lContext) == eSectionCacheItemDeleteResult.notdeleted) lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
                    mNonPersistentKeyItems = lNonPersistentKeyItems;
                }
                else
                {
                    var lNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cItem>();

                    foreach (var lPair in mNonPersistentKeyItems)
                    {
                        if (lPair.Key.UID == null)
                        {
                            if (
                                lPair.Key.Message.IsValid ||
                                lPair.Value.TryDelete(-1, lContext) == eSectionCacheItemDeleteResult.notdeleted
                                )
                                lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
                        }
                        else
                        {
                            if (!lPair.Value.AssignedPersistentKey)
                            {
                                if (
                                    !ZZTryAssignPersistentKey(new cSectionCachePersistentKey(lPair.Key), lPair.Value, lContext) &&
                                    lPair.Value.TryDelete(-1, lContext) == eSectionCacheItemDeleteResult.notdeleted
                                    )
                                    lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
                            }
                        }
                    }

                    mNonPersistentKeyItems = lNonPersistentKeyItems;
                }
            }
        }

        // called by readerwriter when the write is finished
        private void ZAdd(cSectionCachePersistentKey pKey, cItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAdd), pKey, pItem);

            if (ReferenceEquals(pKey, null)) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));

            lock (mLock)
            {
                if (ZZTryGetExistingItem(pKey, true, out var lItem, lContext))
                {
                    if (lItem.TryTouch(lContext))
                    {
                        lContext.TraceVerbose("found existing un-deleted item: {0}", lItem);
                        return;
                    }

                    lContext.TraceVerbose("overwriting deleted item: {0}", lItem);
                    mPersistentKeyItems[pKey] = pItem;
                }
                else
                {
                    lContext.TraceVerbose("adding new item");
                    mPersistentKeyItems.Add(pKey, pItem);
                }

                pItem.SetCached(pKey, lContext);
            }

            try { ItemAdded(pItem); }
            catch { }
        }

        // called by readerwriter when the write is finished
        private void ZAdd(cSectionCacheNonPersistentKey pKey, cItem pItem, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAdd), pKey, pItem);

            if (ReferenceEquals(pKey, null)) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));

            lock (mLock)
            {
                if (!pKey.Message.IsValid) return;

                if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem))
                {
                    if (lItem.TryTouch(lContext))
                    {
                        lContext.TraceVerbose("found existing un-deleted item: {0}", lItem);
                        return;
                    }

                    lContext.TraceVerbose("overwriting deleted item: {0}", lItem);
                    mNonPersistentKeyItems[pKey] = pItem;
                }
                else
                {
                    lContext.TraceVerbose("adding new item");
                    mNonPersistentKeyItems.Add(pKey, pItem);
                }

                pItem.SetCached(pKey, lContext);
            }

            try { ItemAdded(pItem); }
            catch { }
        }

        // called from this class
        private bool ZZTryAssignPersistentKey(cSectionCachePersistentKey pKey, cItem pItem, cTrace.cContext pParentContext)
        {
            // must be called inside the lock
            //  returns true even if the assign fails, as long as the assign was possible

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZZTryAssignPersistentKey), pKey, pItem);

            if (ZZTryGetExistingItem(pKey, false, out var lItem, lContext))
            {
                if (!ReferenceEquals(lItem, pItem))
                {
                    if (lItem.TryTouch(lContext))
                    {
                        lContext.TraceVerbose("found existing un-deleted item: {0}", lItem);
                        return false;
                    }

                    lContext.TraceVerbose("overwriting deleted item: {0}", lItem);
                    mPersistentKeyItems[pKey] = pItem;
                }
            }
            else
            {
                lContext.TraceVerbose("adding new item");
                mPersistentKeyItems.Add(pKey, pItem);
            }

            pItem.TryAssignPersistentKey(pKey, lContext);
            return true;
        }

        // called from this class
        private bool ZZTryGetExistingItem(cSectionCachePersistentKey pKey, bool pLookInNonPersistentKeyItems, out cItem rItem, cTrace.cContext pParentContext)
        {
            // must be called inside the lock
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZZTryGetExistingItem), pKey, pLookInNonPersistentKeyItems);

            if (mPersistentKeyItems.TryGetValue(pKey, out rItem))
            {
                lContext.TraceVerbose("found in persistent-key list: {0}", rItem);
                return true;
            }

            if (!Temporary && TryGetExistingItem(pKey, out rItem))
            {
                lContext.TraceVerbose("found in cache: {0}", rItem);
                if (rItem == null || !rItem.AssignedPersistentKey) throw new cUnexpectedSectionCacheActionException(lContext);
                mPersistentKeyItems.Add(pKey, rItem);
                return true;
            }

            if (!pLookInNonPersistentKeyItems) return false;

            foreach (var lPair in mNonPersistentKeyItems)
            {
                if (lPair.Key == pKey)
                {
                    lContext.TraceVerbose("found in non-persistent-key list: {0}", lPair.Value);
                    rItem = lPair.Value;
                    mPersistentKeyItems.Add(pKey, lPair.Value);
                    return true;
                }
            }

            return false;
        }
    }
}