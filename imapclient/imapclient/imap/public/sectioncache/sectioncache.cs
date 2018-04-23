using System;
using System.Collections.Generic;
using System.IO;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cSectionCache : IDisposable
    {
        private bool mDisposing = false;
        private bool mDisposed = false;
        protected internal readonly string mInstanceName;
        protected readonly cTrace.cContext RootContext;
        private readonly object mLock = new object();
        private readonly Dictionary<cSectionCachePersistentKey, cSectionCacheItem> mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cSectionCacheItem>();
        private readonly Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem> mNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();

        public readonly bool Temporary;
        private readonly cBatchSizer mWriteSizer;

        protected cSectionCache(bool pTemporary, cBatchSizerConfiguration pWriteConfiguration = null, string pInstanceName = "work.bacome.cSectionCache")
        {
            Temporary = pTemporary;
            mWriteSizer = new cBatchSizer(pWriteConfiguration ?? new cBatchSizerConfiguration(1000, 100000, 1000, 1000));
            mInstanceName = pInstanceName;
            RootContext = cMailClient.Trace.NewRoot(pInstanceName);
            RootContext.TraceInformation("cSectionCache by bacome version {0}, release date {1}", cMailClient.Version, cMailClient.ReleaseDate);
        }

        // WILL be called inside the lock
        // MAY be called while the object is disposing
        //
        protected abstract bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem);

        // asks the cache to create a new item
        //  the item will either be deleted during creation by internal code
        //   i.e.
        //    if the retrieval fails
        //    if the item is a duplicate by the time the retrieval finishes
        //    if the cache is disposing by the time the retrieval finishes and it doesn't have a pk
        //  OR newitemadded will be called
        //
        // WILL be called inside the lock
        // WILL NOT be called once the object is disposing
        //
        protected abstract cSectionCacheItem GetNewItem();

        // to increase the number of files and the total size of the cache
        //  WILL be called inside the lock
        //  WILL NOT be called once the object is disposing (even if new items are added to the cache)
        //  NOTE that when this is called the item being added could be either still open or be closed
        //
        protected abstract void NewItemAdded(cSectionCacheItem pItem);

        // required to let the cache know that if it is over budget, then now might be a good time to try doing a tidy up
        //  called outside the lock
        protected internal abstract void ItemClosed();

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
        //   if the cache is temporary, sort by the ones you most want to delete and use TryDelete(change-sequence) on them
        //   if the cache is permanent, sort by the items you most want to delete, if there is an item use TryDelete(change-sequence) else use 
        //    TryGetItem(pk) and then TryDelete(0)
        //
        //
        protected List<cSectionCacheItem> GetAllItems()
        {
            var lItems = new List<cSectionCacheItem>();

            lock (mLock)
            {
                foreach (var lPair in mPersistentKeyItems) lItems.Add(lPair.Value);
                foreach (var lPair in mNonPersistentKeyItems) lItems.Add(lPair.Value);
            }

            return lItems;
        }

        internal bool TryGetReader(cSectionCachePersistentKey pKey, out cSectionCacheItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetReader), pKey);

            lock (mLock)
            {
                if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
                if (ZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem.TryGetReader(out rReader, lContext);
                rReader = null;
                return false;
            }
        }

        internal bool TryGetReader(cSectionCacheNonPersistentKey pKey, out cSectionCacheItem.cReader rReader, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetReader), pKey);

            lock (mLock)
            {
                if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
                if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
                rReader = null;
                return false;
            }
        }

        internal cSectionCacheItem.cReaderWriter GetReaderWriter(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetReaderWriter), pKey);

            lock (mLock)
            {
                if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
                var lItem = GetNewItem();
                if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
                return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
            }
        }

        internal cSectionCacheItem.cReaderWriter GetReaderWriter(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetReaderWriter), pKey);

            lock (mLock)
            {
                if (mDisposing || mDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
                var lItem = GetNewItem();
                if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
                return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
            }
        }

        internal void Add(cSectionCachePersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Add), pKey, pItem);

            if (ReferenceEquals(pKey, null)) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));

            lock (mLock)
            {
                if (mDisposing || mDisposed) return;

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

        internal void Add(cSectionCacheNonPersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            // if the uid is available the other one should have been called

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Add), pKey, pItem);

            if (ReferenceEquals(pKey, null)) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));

            lock (mLock)
            {
                if (mDisposing || mDisposed) return;

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

        internal bool TryAssignPersistentKey(cSectionCachePersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            // note that this returns true even if the assign fails, as long as the assign was possible

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryAssignPersistentKey), pKey, pItem);

            lock (mLock)
            {
                cSectionCacheItem lItem;

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

                pItem.TryAssignPersistentKey(pKey, lContext);

                return true;
            }
        }

        internal bool IsDisposing => mDisposing;

        private bool ZTryGetExistingItem(cSectionCachePersistentKey pKey, bool pLookInNonPersistentKeyItems, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            // must be called inside the lock
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZTryGetExistingItem), pKey, pLookInNonPersistentKeyItems);

            if (mPersistentKeyItems.TryGetValue(pKey, out rItem))
            {
                lContext.TraceVerbose("found in persistent-key list: {0}", rItem);
                return true;
            }

            try
            {
                if (TryGetExistingItem(pKey, out rItem))
                {
                    lContext.TraceVerbose("found in cache: {0}", rItem);
                    if (rItem == null || !rItem.AssignedPersistentKey) throw new cUnexpectedSectionCacheActionException(lContext);
                    mPersistentKeyItems.Add(pKey, rItem);
                    return true;
                }
            }
            catch when (mDisposing) { }

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

        /**<summary></summary>*/
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /**<summary></summary>*/
        protected virtual void Dispose(bool pDisposing)
        {
            if (mDisposed) return;

            if (pDisposing)
            {
                var lContext = RootContext.NewMethod(nameof(cSectionCache), nameof(Dispose), pDisposing);

                lock (mLock)
                {
                    mDisposing = true;
                }

                if (Temporary)
                {
                    foreach (var lItem in mPersistentKeyItems) lItem.Value.TryDelete(-1, lContext);
                    foreach (var lItem in mNonPersistentKeyItems) lItem.Value.TryDelete(-1, lContext);
                }
                else
                {
                    foreach (var lItem in mNonPersistentKeyItems) if (!lItem.Value.AssignedPersistentKey && (lItem.Key.UID == null || !TryAssignPersistentKey(new cSectionCachePersistentKey(lItem.Key), lItem.Value, lContext))) lItem.Value.TryDelete(-1, lContext);
                }
            }

            mDisposed = true;
        }
    }
}