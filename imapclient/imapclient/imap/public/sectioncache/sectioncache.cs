using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCache
    {
        private readonly object mLock = new object();
        public readonly bool Temporary;
        private Dictionary<cSectionCachePersistentKey, cItem> mPersistentKeyItems;
        private Dictionary<cNonPersistentKey, cItem> mNonPersistentKeyItems;

        private int mOpenAccessorCount = 0;

        protected cSectionCache(bool pTemporary)
        {
            Temporary = pTemporary;
            mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cItem>();
            mNonPersistentKeyItems = new Dictionary<cNonPersistentKey, cItem>();
        }

        // asks the cache if it has an item for the key
        //  WILL be called inside the lock
        //  DO NOT call directly (use the other TryGetExistingItem)
        //
        protected virtual bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cItem rItem, cTrace.cContext pParentContext)
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
        protected abstract cItem GetNewItem(cTrace.cContext pParentContext);

        // lets the cache know that a new item has been written
        //  allows the cache to increase the number of files in use and the total size of the cache
        //  NOTE that when this is called the item being added could be either still open or be closed
        //  WILL be called inside the lock 
        //
        protected virtual void ItemAdded(cItem pItem, cTrace.cContext pParentContext) { }

        // lets the cache know that a cached item has been deleted
        //  allows the cache to decrease the number of files in use and the total size of the cache
        //
        protected virtual void ItemDeleted(cItem pItem, cTrace.cContext pParentContext) { }

        // lets the cache know that a previously open item has been closed
        //  if the cache is over budget, then it might be a good time to allow a cache trim to run
        //
        protected virtual void ItemClosed(cTrace.cContext pParentContext) { }

        // for use in cache trimming
        //
        //  NOTE that the following should be done on a separate thread, single threaded
        //
        //  if the cache is over budget
        //   getsectioncacheitems (a)
        //   sort the cache's internal list by the order in which the items should be deleted
        //   foreach item in the internal list
        //    if the cache is under budget break;
        //    use the dictionary from (a) to get the item (b)
        //    if there is no item in the dictionary use getsectioncacheitem to get one (b)
        //    b.trydelete
        //
        protected Dictionary<object, cSectionCacheItem> GetSectionCacheItems(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetSectionCacheItems));

            var lResult = new Dictionary<object, cSectionCacheItem>();

            lock (mLock)
            {
                foreach (var lPair in mNonPersistentKeyItems)
                {
                    var lItem = lPair.Value;

                    if (!lItem.Deleted && !lItem.AssignedPersistentKey)
                    {
                        var lValue = new cSectionCacheItem(lItem, false); // must be done before getting the key
                        var lKey = lItem.GetItemKey() ?? throw new cUnexpectedSectionCacheActionException(lContext, 1); // must be done after snapshotting the changeseq
                        lResult[lKey] = lValue;
                    }
                }

                foreach (var lPair in mPersistentKeyItems)
                {
                    var lItem = lPair.Value;

                    if (!lItem.Deleted)
                    {
                        var lValue = new cSectionCacheItem(lItem, false); // must be done before getting the key
                        var lKey = lItem.GetItemKey() ?? throw new cUnexpectedSectionCacheActionException(lContext, 2); // must be done after snapshotting the changeseq
                        lResult[lKey] = lValue; // the key/ value pair could be in both dictionaries
                    }
                }
            }

            return lResult;
        }

        // for use in cache trimming
        //
        protected cSectionCacheItem GetSectionCacheItem(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetSectionCacheItem), pKey);

            lock (mLock)
            {
                if (ZZTryGetExistingItem(pKey, true, out var lItem, lContext)) return new cSectionCacheItem(lItem, true);
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
        private bool ZTryGetItemReader(cSectionCachePersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetItemReader), pKey);

            lock (mLock)
            {
                if (ZZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        // called by accessor
        private bool ZTryGetItemReader(cNonPersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetItemReader), pKey);

            lock (mLock)
            {
                if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        // called by accessor
        private cItem.cReaderWriter ZGetItemReaderWriter(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetItemReaderWriter));
            var lItem = GetNewItem(lContext);
            if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
            return lItem.GetReaderWriter(lContext);
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
                    foreach (var lPair in mPersistentKeyItems) if (!lPair.Value.TryDelete(-1, lContext)) lPersistentKeyItems.Add(lPair.Key, lPair.Value);
                    mPersistentKeyItems = lPersistentKeyItems;

                    var lNonPersistentKeyItems = new Dictionary<cNonPersistentKey, cItem>();
                    foreach (var lPair in mNonPersistentKeyItems) if (!lPair.Value.TryDelete(-1, lContext)) lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
                    mNonPersistentKeyItems = lNonPersistentKeyItems;
                }
                else
                {
                    var lNonPersistentKeyItems = new Dictionary<cNonPersistentKey, cItem>();

                    foreach (var lPair in mNonPersistentKeyItems)
                    {
                        if (lPair.Key.UID == null)
                        {
                            if (!lPair.Value.TryDelete(-1, lContext)) lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
                        }
                        else
                        {
                            if (!lPair.Value.AssignedPersistentKey)
                            {
                                if (!ZZTryAssignPersistentKey(new cSectionCachePersistentKey(lPair.Key), lPair.Value, lContext) && !lPair.Value.TryDelete(-1, lContext)) lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
                            }
                        }
                    }

                    mNonPersistentKeyItems = lNonPersistentKeyItems;
                }
            }
        }

        // called by readerwriter when the write is finished
        private void ZAddItem(cSectionCachePersistentKey pKey, cItem pItem, long pLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));

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

                pItem.SetCached(pLength, pKey, lContext);

                try { ItemAdded(pItem, lContext); }
                catch { }
            }
        }

        // called by readerwriter when the write is finished
        private void ZAddItem(cNonPersistentKey pKey, cItem pItem, long pLength, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));

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

                pItem.SetCached(pLength, pKey, lContext);

                try { ItemAdded(pItem, lContext); }
                catch { }
            }
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
                lContext.TraceVerbose("found in list: {0}", rItem);
                return true;
            }

            if (!Temporary && TryGetExistingItem(pKey, out rItem, lContext))
            {
                lContext.TraceVerbose("found in cache: {0}", rItem);
                if (rItem == null || !rItem.AssignedPersistentKey) throw new cUnexpectedSectionCacheActionException(lContext);
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