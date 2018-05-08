using System;
using System.Collections.Generic;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCache
    {
        private readonly object mLock = new object();
        public readonly bool Temporary;
        private Dictionary<cSectionCachePersistentKey, cSectionCacheItem> mPersistentKeyItems;
        private Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem> mNonPersistentKeyItems;
        private int mOpenAccessorCount = 0;

        protected cSectionCache(bool pTemporary)
        {
            Temporary = pTemporary;
            mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cSectionCacheItem>();
            mNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();
        }

        // asks the cache if it has an item for the key
        //  WILL be called inside the lock
        //  DO NOT call directly (use the other TryGetExistingItem)
        //
        protected virtual bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
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
        protected abstract cSectionCacheItem GetNewItem(cTrace.cContext pParentContext);

        // lets the cache know that a new item has been written
        //  allows the cache to increase the number of files in use and the total size of the cache
        //  NOTE that when this is called the item being added could be either still open or be closed
        //  WILL be called inside the lock 
        //
        protected internal virtual void ItemAdded(cSectionCacheItem pItem, cTrace.cContext pParentContext) { }

        // lets the cache know that a cached item has been deleted
        //  allows the cache to decrease the number of files in use and the total size of the cache
        //
        protected internal virtual void ItemDeleted(cSectionCacheItem pItem, cTrace.cContext pParentContext) { }

        // lets the cache know that a previously open item has been closed
        //  if the cache is over budget, then it might be a good time to allow a cache trim to run
        //
        protected internal virtual void ItemClosed(cTrace.cContext pParentContext) { }

        // for use in cache trimming
        //
        //  NOTE that the following should be done on a separate thread, single threaded
        //
        //  if the cache is over budget
        //   getitemsnapshots (a)
        //   sort the cache's internal list by the order in which the items should be deleted
        //   foreach item in the internal list
        //    if the cache is under budget break;
        //    use the dictionary from (a) to get the itemsnapshot (b)
        //    if there is no itemsnapshot (b) use getitemsnapshot to get one (b)
        //    b.trydelete [won't delete the item if it has been touched since the snapshot]
        //
        protected Dictionary<object, cSectionCacheItemSnapshot> GetItemSnapshots(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetItemSnapshots));

            var lResult = new Dictionary<object, cSectionCacheItemSnapshot>();

            lock (mLock)
            {
                foreach (var lPair in mNonPersistentKeyItems)
                {
                    var lItem = lPair.Value;

                    if (!lItem.Deleted)
                    {
                        var lValue = new cSectionCacheItemSnapshot(lItem, false); // must be done before getting the key
                        var lKey = lItem.ItemKey ?? throw new cUnexpectedSectionCacheActionException(lContext, 1); // must be done after snapshotting the changeseq
                        lResult[lKey] = lValue;
                    }
                }

                foreach (var lPair in mPersistentKeyItems)
                {
                    var lItem = lPair.Value;

                    if (!lItem.Deleted)
                    {
                        var lValue = new cSectionCacheItemSnapshot(lItem, false); // must be done before getting the key
                        var lKey = lItem.ItemKey ?? throw new cUnexpectedSectionCacheActionException(lContext, 2); // must be done after snapshotting the changeseq
                        lResult[lKey] = lValue; // the key/ value pair could be in both dictionaries
                    }
                }
            }

            return lResult;
        }

        // for use in cache trimming
        //
        protected cSectionCacheItemSnapshot GetItemSnapshot(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetItemSnapshot), pKey);

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, true, out var lItem, lContext)) return new cSectionCacheItemSnapshot(lItem, true);
            }

            return null;
        }

        internal bool IsClosed => mOpenAccessorCount == 0;

        internal cAccessor GetAccessor(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetAccessor));

            cAccessor lAccessor;

            lock (mLock)
            {
                lAccessor = new cAccessor(this, ZDecrementOpenAccessorCount, lContext);
                mOpenAccessorCount++;
            }

            return lAccessor;
        }

        private bool ZTryGetItemReader(cSectionCachePersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetItemReader), pKey);

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        private bool ZTryGetItemReader(cSectionCacheNonPersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetItemReader), pKey);

            lock (mLock)
            {
                if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        private void ZAddItem(cSectionCachePersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, true, out var lItem, lContext))
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
        }

        private void ZAddItem(cSectionCacheNonPersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            lock (mLock)
            {
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
        }

        // called by accessor when it is disposed
        private void ZDecrementOpenAccessorCount(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZDecrementOpenAccessorCount));

            lock (mLock)
            {
                if (--mOpenAccessorCount != 0) return;

                var lPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cSectionCacheItem>();
                foreach (var lPair in mPersistentKeyItems) if ((Temporary || !lPair.Value.PersistentKeyAssigned) && !lPair.Value.TryDelete(-1, lContext)) lPersistentKeyItems.Add(lPair.Key, lPair.Value);
                mPersistentKeyItems = lPersistentKeyItems;

                var lNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();
                foreach (var lPair in mNonPersistentKeyItems) if (!lPair.Value.PersistentKeyAssigned && !lPair.Value.TryDelete(-1, lContext)) lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
                mNonPersistentKeyItems = lNonPersistentKeyItems;
            }
        }

        private bool ZTryGetExistingItem(cSectionCachePersistentKey pKey, bool pLookInNonPersistentKeyItems, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            // must be called inside the lock
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetExistingItem), pKey, pLookInNonPersistentKeyItems);

            if (mPersistentKeyItems.TryGetValue(pKey, out rItem))
            {
                lContext.TraceVerbose("found in list: {0}", rItem);
                return true;
            }

            if (!Temporary && TryGetExistingItem(pKey, out rItem, lContext))
            {
                lContext.TraceVerbose("found in cache: {0}", rItem);
                if (rItem == null || !rItem.IsExistingItem) throw new cUnexpectedSectionCacheActionException(lContext);
                mPersistentKeyItems.Add(pKey, rItem);
                return true;
            }

            if (!pLookInNonPersistentKeyItems) return false;

            foreach (var lPair in mNonPersistentKeyItems)
            {
                if (pKey.Equals(lPair.Key))
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