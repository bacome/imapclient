using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCache : IDisposable
    {
        private bool mDisposed = false;
        private bool mDisposing = false;
        public readonly string InstanceName;
        public readonly int MaintenanceFrequency;
        protected readonly cTrace.cContext mRootContext;
        private readonly object mLock = new object();
        private Dictionary<cSectionCachePersistentKey, cSectionCacheItem> mPersistentKeyItems;
        private Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem> mNonPersistentKeyItems;
        private int mChangeSequence = 0;

        private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource();
        private readonly Task mBackgroundTask = null;

        protected cSectionCache(string pInstanceName, int pMaintenanceFrequency)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            if (MaintenanceFrequency < 1) throw new ArgumentOutOfRangeException(nameof(pMaintenanceFrequency));
            MaintenanceFrequency = pMaintenanceFrequency;
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
            mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cSectionCacheItem>();
            mNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();
            mBackgroundTask = ZBackgroundTaskAsync(mBackgroundCancellationTokenSource.Token, mRootContext);
        }

        // asks the cache to create a new item
        //  the item will either be deleted during creation by internal code
        //   i.e.
        //    if the retrieval fails
        //    if the item is a duplicate by the time the retrieval finishes
        //    if the item doesn't have a pk and the npk is no longer valid
        //  OR itemadded will be called
        //
        protected abstract cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext);

        // asks the cache if it has an item for the key
        //  DO NOT call directly (use the other TryGetExistingItem)
        //
        protected virtual bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetExistingItem), pKey);
            rItem = null;
            return false;
        }

        // reconcile the actually cached items with the sectioncacheitems modifying the cacheitems as required
        //
        protected virtual void Reconcile(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Reconcile));
        }

        // to trim the cache if it is required
        //  if the cache is over budget
        //   sort the cache's internal item list by the order in which the items should be deleted
        //   foreach item in the internal list
        //    if the cache is under budget break;
        //    use the dictionary to get the snapshot
        //    if there is no snapshot use getsnapshot to get one
        //    call snapshot.trydelete [won't delete the item if it has been touched since the snapshot]
        //    [if the item is deleted expect itemdeleted to be called]
        //
        protected virtual void Maintenance(Dictionary<string, cSectionCacheItemSnapshot> pSnapshots, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Maintenance));
        }

        public bool IsDisposed => mDisposed || mDisposing;

        // for use in tidy
        //
        protected cSectionCacheItemSnapshot GetSnapshot(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetSnapshot), pKey);

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, false, out var lItem, lContext)) return new cSectionCacheItemSnapshot(lItem, true);
            }

            return null;
        }

        internal void Changed(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Changed));
            Interlocked.Increment(ref mChangeSequence);
        }

        internal bool TryGetItemReader(cSectionCachePersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pKey);

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, true, out var lItem, lContext)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        internal bool TryGetItemReader(cSectionCacheNonPersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pKey);

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            lock (mLock)
            {
                if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        internal cSectionCacheItem GetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewItem));

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            lock (mLock)
            {
                return YGetNewItem(lContext);
            }
        }

        private void ZAddItem(cSectionCachePersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, false, out var lItem, lContext))
                {
                    if (lItem.TryTouch(lContext))
                    {
                        lContext.TraceVerbose("found existing un-deleted item: {0}", lItem);
                        return;
                    }

                    lContext.TraceVerbose("overwriting deleted item: {0}", lItem);
                }
                else lContext.TraceVerbose("adding new item");

                mPersistentKeyItems[pKey] = pItem;
                pItem.SetCached(pKey, lContext);
            }
        }

        private void ZAddItem(cSectionCacheNonPersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

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
                }
                else lContext.TraceVerbose("adding new item");

                mNonPersistentKeyItems[pKey] = pItem;
                pItem.SetCached(pKey, lContext);
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

            if (TryGetExistingItem(pKey, out rItem, lContext))
            {
                lContext.TraceVerbose("found in cache: {0}", rItem);
                if (rItem == null || !rItem.Cached) throw new cUnexpectedSectionCacheActionException(lContext);
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
                    lPair.Value.SetIndexed(lContext);
                    return true;
                }
            }

            return false;
        }

        private async Task ZBackgroundTaskAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZBackgroundTaskAsync));

            var lLastChangeSequence = mChangeSequence;

            try
            {
                while (true)
                {
                    lock (mLock)
                    {
                        try { Reconcile(pCancellationToken, lContext); }
                        catch (Exception e)
                        {
                            ;?;
                        }
                    }

                    if (mChangeSequence != lLastChangeSequence)
                    {
                        lLastChangeSequence = mChangeSequence;
                        ZMaintenance(pCancellationToken, lContext);
                    }

                    lContext.TraceVerbose("waiting: {0}", MaintenanceFrequency);
                    await Task.Delay(MaintenanceFrequency, pCancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (!pCancellationToken.IsCancellationRequested && lContext.TraceException("the background task is stopping due to an unexpected error", e)) { }
        }

        private void ZMaintenance(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZMaintenance));

            // delete duplicates and invalids, index items that can be, tidy up the npk array

            var lToDelete = new List<cSectionCacheItem>();

            lock (mLock)
            {
                var lNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();

                foreach (var lPair in mNonPersistentKeyItems)
                {
                    var lItem = lPair.Value;

                    if (lItem.Deleted || lItem.Indexed) continue;

                    if (lItem.PersistentKey == null)
                    {
                        lNonPersistentKeyItems.Add(lPair.Key, lItem);

                        if (mDisposing)
                        {
                            lContext.TraceVerbose("found item with no peristent key while disposing: {0}", lItem);
                            lToDelete.Add(lItem);
                        }
                        else if (!lPair.Key.IsValid)
                        {
                            lContext.TraceVerbose("found item with invalid key: {0}", lItem);
                            lToDelete.Add(lItem);
                        }

                        continue;
                    }

                    if (ZTryGetExistingItem(lItem.PersistentKey, false, out var lExistingItem, lContext))
                    {
                        var lTouched = lExistingItem.TryTouch(lContext);

                        if (pCancellationToken.IsCancellationRequested) return;

                        if (lTouched)
                        {
                            lNonPersistentKeyItems.Add(lPair.Key, lItem);
                            lContext.TraceVerbose("found duplicate item: {0}", lItem);
                            lToDelete.Add(lItem);
                            continue;
                        }
                    }

                    lContext.TraceVerbose("indexing item: {0}", lItem);
                    mPersistentKeyItems[lItem.PersistentKey] = lItem;
                    lItem.SetIndexed(lContext);
                }

                mNonPersistentKeyItems = lNonPersistentKeyItems;
            }

            if (pCancellationToken.IsCancellationRequested) return;

            foreach (var lItem in lToDelete)
            {
                ;?; // the reconcile could then add it back?
                lItem.TryDelete(-1, lContext);
                if (pCancellationToken.IsCancellationRequested) return;
            }

            // assign pk loop, tidy up the pk array

            var lToAssignPersistentKey = new List<cSectionCacheItem>();

            lock (mLock)
            {
                var lPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cSectionCacheItem>();

                foreach (var lPair in mPersistentKeyItems)
                {
                    var lItem = lPair.Value;
                    if (lItem.Deleted) continue;
                    lPersistentKeyItems.Add(lPair.Key, lItem);
                    if (!lItem.PersistentKeyAssigned) lToAssignPersistentKey.Add(lItem);
                }

                mPersistentKeyItems = lPersistentKeyItems;
            }

            if (pCancellationToken.IsCancellationRequested) return;

            foreach (var lItem in lToAssignPersistentKey)
            {
                lItem.TryAssignPersistentKey(lContext);
                if (!lItem.PersistentKeyAssigned && mDisposing) lItem.TryDelete(-1, lContext);
                if (pCancellationToken.IsCancellationRequested) return;
            }

            // generate snapshot for cache maintenance

            var lSnapshots = new Dictionary<string, cSectionCacheItemSnapshot>();

            lock (mLock)
            {
                foreach (var lPair in mPersistentKeyItems)
                {
                    var lItem = lPair.Value;
                    if (!lItem.Deleted) lSnapshots.Add(lItem.ItemKey, new cSectionCacheItemSnapshot(lItem, false)); 
                }

                foreach (var lPair in mNonPersistentKeyItems)
                {
                    var lItem = lPair.Value;
                    if (!lItem.Deleted && !lItem.Indexed) lSnapshots.Add(lItem.ItemKey, new cSectionCacheItemSnapshot(lItem, false));
                }
            }

            if (pCancellationToken.IsCancellationRequested) return;

            Maintenance(lSnapshots, pCancellationToken, lContext);
        }

        public override string ToString() => $"{nameof(cSectionCache)}({InstanceName})";

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool pDisposing)
        {
            if (mDisposed) return;

            if (pDisposing)
            {
                mDisposing = true;

                if (mBackgroundCancellationTokenSource != null && !mBackgroundCancellationTokenSource.IsCancellationRequested)
                {
                    try { mBackgroundCancellationTokenSource.Cancel(); }
                    catch { }
                }

                if (mBackgroundTask != null)
                {
                    // wait for the task to exit before disposing it
                    try { mBackgroundTask.Wait(); }
                    catch { }

                    try { mBackgroundTask.Dispose(); }
                    catch { }
                }

                if (mBackgroundCancellationTokenSource != null)
                {
                    try { mBackgroundCancellationTokenSource.Dispose(); }
                    catch { }
                }

                try
                {
                    var lContext = mRootContext.NewMethod(nameof(cSectionCache), nameof(Dispose));
                    try { ZMaintenance(CancellationToken.None, lContext); }
                    catch (Exception e) { lContext.TraceException(e); }
                }
                catch { }
            }

            mDisposed = true;
        }
    }
}