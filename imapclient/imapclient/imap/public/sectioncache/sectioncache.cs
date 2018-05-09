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
        public readonly string InstanceName;
        public readonly int DelayAfterTidy;
        protected readonly cTrace.cContext mRootContext;
        private readonly object mLock = new object();
        private Dictionary<cSectionCachePersistentKey, cSectionCacheItem> mPersistentKeyItems;
        private Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem> mNonPersistentKeyItems;
        private int mOpenAccessorCount = 0;

        private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource();
        private readonly cReleaser mBackgroundReleaser;
        private readonly Task mBackgroundTask = null;

        protected cSectionCache(string pInstanceName, int pDelayAfterTidy)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            if (pDelayAfterTidy < 0) throw new ArgumentOutOfRangeException(nameof(pDelayAfterTidy));
            DelayAfterTidy = pDelayAfterTidy;
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
            mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cSectionCacheItem>();
            mNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();
            mBackgroundReleaser = new cReleaser(pInstanceName, mBackgroundCancellationTokenSource.Token);
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
        protected abstract cSectionCacheItem GetNewItem(cTrace.cContext pParentContext);

        // asks the cache if it has an item for the key
        //  WILL be called inside the lock
        //  DO NOT call directly (use the other TryGetExistingItem)
        //
        protected virtual bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetExistingItem), pKey);
            rItem = null;
            return false;
        }

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
        protected virtual void Tidy(Dictionary<object, cSectionCacheItemSnapshot> pSnapshots, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Tidy));
        }

        protected void TriggerTidy(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TriggerTidy));
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            mBackgroundReleaser.Release(lContext);
        }

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

        internal bool IsClosed => mOpenAccessorCount == 0;

        internal cAccessor GetAccessor(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetAccessor));

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

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

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

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

            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            lock (mLock)
            {
                if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
            }

            rReader = null;
            return false;
        }

        private cSectionCacheItem ZGetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetNewItem));
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            return GetNewItem(lContext);
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

            mBackgroundReleaser.Release(lContext);
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

            mBackgroundReleaser.Release(lContext);
        }

        // called by accessor when it is disposed
        private void ZDecrementOpenAccessorCount(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZDecrementOpenAccessorCount));

            lock (mLock)
            {
                if (--mOpenAccessorCount != 0) return;

                var lPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cSectionCacheItem>();
                foreach (var lPair in mPersistentKeyItems) if (!lPair.Value.Deleted && !lPair.Value.PersistentKeyAssigned && !lPair.Value.TryDelete(-1, lContext)) lPersistentKeyItems.Add(lPair.Key, lPair.Value);
                mPersistentKeyItems = lPersistentKeyItems;

                var lNonPersistentKeyItems = new Dictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();
                foreach (var lPair in mNonPersistentKeyItems) if (!lPair.Value.Deleted && !lPair.Value.PersistentKeyAssigned && !lPair.Value.TryDelete(-1, lContext)) lNonPersistentKeyItems.Add(lPair.Key, lPair.Value);
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

        private async Task ZBackgroundTaskAsync( CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZBackgroundTaskAsync));

            try
            {
                while (true)
                {
                    lContext.TraceVerbose("waiting for release");
                    await mBackgroundReleaser.GetAwaitReleaseTask(lContext).ConfigureAwait(false);

                    mBackgroundReleaser.Reset(lContext);

                    var lSnapshots = new Dictionary<object, cSectionCacheItemSnapshot>();
                    var lToDeletes = new List<cSectionCacheItemSnapshot>();

                    lock (mLock)
                    {
                        foreach (var lPair in mPersistentKeyItems)
                        {
                            var lItem = lPair.Value;

                            if (!lItem.Deleted)
                            {
                                var lSnapshot = new cSectionCacheItemSnapshot(lItem, false); // must be done before getting the key
                                lSnapshots.Add(lItem.ItemKey, lSnapshot); 
                            }
                        }

                        foreach (var lPair in mNonPersistentKeyItems)
                        {
                            var lItem = lPair.Value;

                            if (!lItem.Deleted && !lItem.Indexed)
                            {
                                var lSnapshot = new cSectionCacheItemSnapshot(lItem, false); // must be done before getting the key
                                lSnapshots.Add(lItem.ItemKey, lSnapshot); 

                                if (lItem.PersistentKey == null)
                                {
                                    if (!lPair.Key.IsValid)
                                    {
                                        lContext.TraceVerbose("found invalid item: {0}", lItem);
                                        lToDeletes.Add(lSnapshot);
                                    }
                                }
                                else
                                {
                                    // try indexing it
                                    if (ZTryGetExistingItem(lItem.PersistentKey, false, out var lExistingItem, lContext))
                                    {
                                        if (lExistingItem.TryTouch(lContext))
                                        {
                                            lContext.TraceVerbose("found duplicate un-indexed item: {0}", lItem);
                                            lToDeletes.Add(lSnapshot);
                                        }
                                        else
                                        {
                                            lContext.TraceVerbose("indexing item: {0}", lItem);
                                            mPersistentKeyItems[lItem.PersistentKey] = lItem;
                                            lItem.SetIndexed(lContext);
                                        }
                                    }
                                    else
                                    {
                                        lContext.TraceVerbose("indexing item: {0}", lItem);
                                        mPersistentKeyItems[lItem.PersistentKey] = lItem;
                                        lItem.SetIndexed(lContext);
                                    }

                                    if (pCancellationToken.IsCancellationRequested) return;
                                }
                            }
                        }
                    }

                    if (pCancellationToken.IsCancellationRequested) return;

                    foreach (var lSnapshot in lToDeletes)
                    {
                        lSnapshot.TryDelete(lContext);
                        if (pCancellationToken.IsCancellationRequested) return;
                    }

                    Tidy(lSnapshots, pCancellationToken, lContext);

                    if (DelayAfterTidy > 0)
                    {
                        lContext.TraceVerbose("waiting: {0}", DelayAfterTidy);
                        await Task.Delay(DelayAfterTidy, pCancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e) when (!pCancellationToken.IsCancellationRequested && lContext.TraceException("the background task is stopping due to an unexpected error", e)) { }
        }

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
                if (mBackgroundCancellationTokenSource != null && !mBackgroundCancellationTokenSource.IsCancellationRequested)
                {
                    try { mBackgroundCancellationTokenSource.Cancel(); }
                    catch { }
                }

                // must dispose first as the background task uses the other objects to be disposed
                if (mBackgroundTask != null)
                {
                    // wait for the task to exit before disposing it
                    try { mBackgroundTask.Wait(); }
                    catch { }

                    try { mBackgroundTask.Dispose(); }
                    catch { }
                }

                if (mBackgroundReleaser != null)
                {
                    try { mBackgroundReleaser.Dispose(); }
                    catch { }
                }

                if (mBackgroundCancellationTokenSource != null)
                {
                    try { mBackgroundCancellationTokenSource.Dispose(); }
                    catch { }
                }
            }

            mDisposed = true;
        }
    }
}