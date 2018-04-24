using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCache
    {
        private readonly object mLock = new object();
        private readonly Dictionary<cSectionCachePersistentKey, cItem> mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cItem>();
        private readonly Dictionary<cSectionCacheNonPersistentKey, cItem> mNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cItem>();

        public readonly bool Temporary;
        private readonly cBatchSizer mWriteSizer;
        protected readonly string mInstanceName;
        protected readonly cTrace.cContext mRootContext;

        private int mOpenAccessorCount = 0;

        protected cSectionCache(bool pTemporary, cBatchSizerConfiguration pWriteConfiguration = null, string pInstanceName = "work.bacome.cSectionCache")
        {
            Temporary = pTemporary;
            mWriteSizer = new cBatchSizer(pWriteConfiguration ?? new cBatchSizerConfiguration(1000, 100000, 1000, 1000));
            mInstanceName = pInstanceName;
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
            mRootContext.TraceInformation("cSectionCache by bacome version {0}, release date {1}", cMailClient.Version, cMailClient.ReleaseDate);
        }

        // asks the cache if it has an item for the key
        //  WILL be called inside the lock
        //  DO NOT call directly (use the other TryGetExistingItem)
        //
        protected abstract bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cItem rItem);

        // asks the cache to create a new item
        //  the item will either be deleted during creation by internal code
        //   i.e.
        //    if the retrieval fails
        //    if the item is a duplicate by the time the retrieval finishes
        //    if the item doesn't have a pk and the npk is no longer valid
        //  OR newitemadded will be called
        //
        // WILL be called inside the lock
        //  DO NOT call directly
        //
        protected abstract cItem GetNewItem();

        // lets the cache know that a new item has been written
        //  allows the cache to increase the number of files in use and the total size of the cache
        //  WILL be called inside the lock
        //  NOTE that when this is called the item being added could be either still open or be closed
        //
        protected abstract void NewItemAdded(cItem pItem);

        // lets the cache know that a previously open item has been closed
        //  if the cache is over budget, then it might be a good time to allow a cache trim to run
        //  called outside the lock
        //
        protected abstract void ItemClosed();

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
                if (ZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem;
            }

            return null;
        }

        internal cAccessor GetAccessor(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetAccessor));

            lock (mLock)
            {
                mOpenAccessorCount++;
                return new cAccessor(this, lContext);
            }
        }

        internal bool IsClosed
        {
            get
            {
                lock (mLock)
                {
                    return mOpenAccessorCount == 0;
                }
            }
        }

        private bool ZTryGetExistingItem(cSectionCachePersistentKey pKey, bool pLookInNonPersistentKeyItems, out cItem rItem, cTrace.cContext pParentContext)
        {
            // must be called inside the lock
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetExistingItem), pKey, pLookInNonPersistentKeyItems);

            if (mPersistentKeyItems.TryGetValue(pKey, out rItem))
            {
                lContext.TraceVerbose("found in persistent-key list: {0}", rItem);
                return true;
            }

            if (TryGetExistingItem(pKey, out rItem))
            {
                lContext.TraceVerbose("found in cache: {0}", rItem);
                if (rItem == null || !rItem.AssignedPersistentKey) throw new cUnexpectedSectionCacheActionException(lContext);
                mPersistentKeyItems.Add(pKey, rItem);
                return true;
            }

            if (!pLookInNonPersistentKeyItems) return false;

            foreach (var lItem in mNonPersistentKeyItems)
            {
                if (lItem.Key == pKey)
                {
                    lContext.TraceVerbose("found in non-persistent-key list: {0}", rItem);
                    rItem = lItem.Value;
                    mPersistentKeyItems.Add(pKey, lItem.Value);
                    return true;
                }
            }

            return false;
        }

        private bool ZTryGetReader(cSectionCachePersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetReader), pKey);

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

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

        private cItem.cReaderWriter ZGetReaderWriter(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetReaderWriter), pKey);

            lock (mLock)
            {
                var lItem = GetNewItem();
                if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
                return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
            }
        }

        private cItem.cReaderWriter ZGetReaderWriter(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetReaderWriter), pKey);

            lock (mLock)
            {
                var lItem = GetNewItem();
                if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
                return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
            }
        }

        private void ZAdd(cSectionCachePersistentKey pKey, cItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAdd), pKey, pItem);

            if (ReferenceEquals(pKey, null)) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));

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

                NewItemAdded(pItem);
            }
        }

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

                NewItemAdded(pItem);
            }
        }

        private bool ZTryAssignPersistentKey(cSectionCachePersistentKey pKey, cItem pItem, cTrace.cContext pParentContext)
        {
            // note that this returns true even if the assign fails, as long as the assign was possible

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryAssignPersistentKey), pKey, pItem);

            lock (mLock)
            {
                cItem lItem;

                if (ZTryGetExistingItem(pKey, false, out lItem, lContext))
                {
                    if (lItem.TryTouch(lContext))
                    {
                        lContext.TraceVerbose("found existing un-deleted item: {0}", lItem);
                        if (!ReferenceEquals(lItem, pItem)) return false;
                    }
                    else
                    {
                        lContext.TraceVerbose("overwriting deleted item: {0}", lItem);
                        mPersistentKeyItems[pKey] = pItem;
                    }
                }
                else
                {
                    lContext.TraceVerbose("adding new item");
                    mPersistentKeyItems.Add(pKey, pItem);
                }
            }

            pItem.TryAssignPersistentKey(pKey, lContext);
            return true;
        }

        private void ZDecrementOpenAccessorCount(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZDecrementOpenAccessorCount));

            lock (mLock)
            {
                mOpenAccessorCount--;

                if (mOpenAccessorCount == 0 && Temporary)
                {
                    foreach (var lItem in mPersistentKeyItems) lItem.Value.TryDelete(-1, lContext);
                    foreach (var lItem in mNonPersistentKeyItems) lItem.Value.TryDelete(-1, lContext);
                }
                else
                {
                    foreach (var lItem in mNonPersistentKeyItems)
                    {
                        if (lItem.Value.AssignedPersistentKey) continue;

                        if (lItem.Key.UID == null)
                        {
                            if (!lItem.Key.Message.IsValid) lItem.Value.TryDelete(-1, lContext);
                        }
                        else
                        {
                            if (!ZTryAssignPersistentKey(new cSectionCachePersistentKey(lItem.Key), lItem.Value, lContext)) lItem.Value.TryDelete(-1, lContext);
                        }
                    }
                }

                // rebuild the arrays to only include the non-deleted items, remove the npks with assigned keys from the array also
                ;?;
            }
        }
    }
}