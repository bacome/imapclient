using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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

        private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource();
        private readonly Task mBackgroundTask = null;

        private readonly ConcurrentDictionary<cSectionCachePersistentKey, cSectionCacheItem> mPersistentKeyItems;
        private readonly ConcurrentDictionary<cSectionCacheNonPersistentKey, cSectionCacheItem> mNonPersistentKeyItems;

        protected cSectionCache(string pInstanceName, int pMaintenanceFrequency)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            if (MaintenanceFrequency < 60000) throw new ArgumentOutOfRangeException(nameof(pMaintenanceFrequency));
            MaintenanceFrequency = pMaintenanceFrequency;
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
            mBackgroundTask = ZBackgroundTaskAsync(mBackgroundCancellationTokenSource.Token, mRootContext);
        }

        // asks the cache to create a new item
        //
        protected abstract cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext);

        // asks the cache if it has an item for the key
        //
        protected virtual bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetExistingItem), pKey);
            rItem = null;
            return false;
        }

        // reconcile the actually cached items with the cache's full item list (as opposed to the lists in this class)
        //  should call setdeleted on items that have been provided to this class but are now removed 
        //  may add new items (to the cache's full item list) [if adding items to the full item list requires maintenance to be called, call setchanged]
        //
        protected virtual void Reconcile(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Reconcile));
        }

        ;?; // pass a trydelete service object that wraps the snapshots
        // to trim the cache if it is required
        //  if the cache is over budget
        //   sort the cache's full item list (as opposed to the lists in this class) by the order in which the items should be deleted
        //   foreach item
        //    if the cache is under budget break;
        //    use the dictionary to get the snapshot for the item
        //    if there is no snapshot in the dictionary use getsnapshot to get one
        //    call snapshot.trydelete [won't delete the item if it has been touched since the snapshot]
        //    [if the item is deleted expect itemdeleted to be called]
        //
        protected virtual void Maintenance(, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Maintenance));
        }

        public bool IsDisposed => mDisposed || mDisposing;

        // for use in tidy
        //
        protected cSectionCacheItemSnapshot GetSnapshotxxxx(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetSnapshot), pKey);

            lock (mLock)
            {
                if (ZTryGetExistingItem(pKey, false, out var lItem, lContext)) return new cSectionCacheItemSnapshot(lItem, true);
            }

            return null;
        }

        internal bool TryGetItemReader(cSectionCachePersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pKey);

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            if (mPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);

            if (TryGetExistingItem(pKey, out var lExistingItem, lContext))
            {
                if (lExistingItem == null || !lExistingItem.Cached) throw new cUnexpectedSectionCacheActionException(lContext);
                var lCachedItem = mPersistentKeyItems.GetOrAdd(pKey, lExistingItem);
                return lCachedItem.TryGetReader(out rReader, lContext);
            }

            foreach (var lPair in mNonPersistentKeyItems)
            {
                if (pKey.Equals(lPair.Key))
                {
                    var lFoundItem = lPair.Value;
                    var lCachedItem = mPersistentKeyItems.GetOrAdd(pKey, lFoundItem);
                    lCachedItem.SetIndexed(lContext);
                    return lCachedItem.TryGetReader(out rReader, lContext);
                }
            }

            rReader = null;
            return false;
        }

        internal bool TryGetItemReader(cSectionCacheNonPersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pKey);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
            rReader = null;
            return false;
        }

        internal cSectionCacheItem GetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewItem));
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            return YGetNewItem(lContext);
        }

        private void ZAddItem(cSectionCachePersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            if (mPersistentKeyItems.TryGetValue(pKey, out var lExistingItem))
            {
                if (lExistingItem.TryTouch(lContext)) return;
                if (mPersistentKeyItems.TryUpdate(pKey, pItem, lExistingItem)) pItem.SetCached(pKey, lContext);
                return;
            }

            if (mPersistentKeyItems.TryAdd(pKey, pItem)) pItem.SetCached(pKey, lContext);
        }

        private void ZAddItem(cSectionCacheNonPersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZAddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            if (mNonPersistentKeyItems.TryGetValue(pKey, out var lExistingItem))
            {
                if (lExistingItem.TryTouch(lContext)) return;
                if (mNonPersistentKeyItems.TryUpdate(pKey, pItem, lExistingItem)) pItem.SetCached(pKey, lContext);
                return;
            }

            if (mNonPersistentKeyItems.TryAdd(pKey, pItem)) pItem.SetCached(pKey, lContext);
        }

        private async Task ZBackgroundTaskAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZBackgroundTaskAsync));

            try
            {
                while (true)
                {
                    ZMaintenance(pCancellationToken, lContext);
                    lContext.TraceVerbose("waiting: {0}", MaintenanceFrequency);
                    await Task.Delay(MaintenanceFrequency, pCancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (!pCancellationToken.IsCancellationRequested && lContext.TraceException("the background task is stopping due to an unexpected error", e)) { }
        }

        private void ZMaintenance(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZMaintenance));

            // delete duplicates and invalids, index items that can be indexed

            foreach (var lPair in mNonPersistentKeyItems)
            {
                var lItem = lPair.Value;

                if (lItem.Deleted || lItem.Indexed) continue;

                if (lItem.PersistentKey == null)
                {
                    if (mDisposing || !lPair.Key.IsValid)
                    {
                        lItem.TryDelete(-1, lContext);
                        if (pCancellationToken.IsCancellationRequested) return;
                    }

                    continue;
                }

                if (mPersistentKeyItems.TryGetValue(lItem.PersistentKey, out var lExistingItem))
                {
                    if (lExistingItem != lItem)
                    {
                        if (lExistingItem.TryTouch(lContext)) lItem.TryDelete(-1, lContext);
                        else if (mPersistentKeyItems.TryUpdate(lItem.PersistentKey, lItem, lExistingItem)) lItem.SetIndexed(lContext);
                        if (pCancellationToken.IsCancellationRequested) return;
                    }
                }
                else if (mPersistentKeyItems.TryAdd(lItem.PersistentKey, lItem)) lItem.SetIndexed(lContext);
            }

            if (pCancellationToken.IsCancellationRequested) return;

            // assign pk loop

            foreach (var lPair in mPersistentKeyItems)
            {
                var lItem = lPair.Value;
                if (lItem.Deleted || lItem.PersistentKeyAssigned) continue;
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