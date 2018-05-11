using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFileBasedSectionCache : cSectionCache
    {
        public readonly long ByteCountBudget;
        public readonly int FileCountBudget;

        private readonly ConcurrentDictionary<string, cFileBasedSectionCacheItem> mNewItems = new ConcurrentDictionary<string, cFileBasedSectionCacheItem>();
        private readonly ConcurrentDictionary<cSectionCachePersistentKey, cFileBasedSectionCacheItem> mPersistentKeyAssignedItems = new ConcurrentDictionary<cSectionCachePersistentKey, cFileBasedSectionCacheItem>();

        public cFileBasedSectionCache(string pInstanceName, int pMaintenanceFrequency, long pByteCountBudget, int pFileCountBudget) : base(pInstanceName, pMaintenanceFrequency)
        {
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));

            FileCountBudget = pFileCountBudget;
            ByteCountBudget = pByteCountBudget;
        }

        protected abstract string YGetNewFileName(cTrace.cContext pParentContext);

        ;?; // and a new override

        protected virtual cFileBasedSectionCacheItem GetNewItem(string pFileName, Stream pStream, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(GetNewItem), pFileName);
            return new cFileBasedSectionCacheItem(this, pFileName, pStream);
        }

        protected virtual Dictionary<cSectionCachePersistentKey, FileInfo> GetExistingItems(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(GetExistingItems));
            return new Dictionary<cSectionCachePersistentKey, FileInfo>();
        }

        sealed protected override cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(YGetNewItem));

            while (true)
            {
                string lFileName = YGetNewFileName(lContext);

                // note that if the cache is running short on space and files are all in use, and you are unlucky that a trim is running, this may fail => do it in a loop
                //  OR: 
                ;?;

                var lStream = new FileStream(lFileName, FileMode.Truncate, FileAccess.ReadWrite, FileShare.Read);

                cFileBasedSectionCacheItem lExistingItem = null;

                try
                {
                    lock (mLock)
                    {
                        if (mAllCachedItems.TryGetValue(lFileName, out lExistingItem) && !lExistingItem.Deleted) lContext.TraceWarning("allocated an in-use file name {0}", lFileName);
                        else return GetNewItem(lFileName, lStream, lContext);
                    }
                }
                catch
                {
                    lStream.Dispose();
                    throw;
                }

                lStream.Dispose();
                lExistingItem.SetDeleted(lContext);
            }
        }

        sealed protected override bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(TryGetExistingItem));

            lock (mLock)
            {
                if (mPersistentKeyItems.TryGetValue(pKey, out var lItem))
                {
                    rItem = lItem;
                    return true;
                }
            }

            rItem = null;
            return false;
        }

        protected sealed override void Reconcile(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(Reconcile));

            lock (mLock)
            {
                GetExistingItems
            }


            base.Reconcile(pCancellationToken, pParentContext);
        }

        protected override void Maintenance(Dictionary<string, cSectionCacheItemSnapshot> pSnapshots, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(Maintenance));

            if (ZOverBudget())
            {
                foreach (var lItem in mAllItems)
                {

                }




                List<cTempFileItem> lItems = new List<cTempFileItem>();

                lock (mLock)
                {
                    lContext.TraceVerbose("getting list of items");

                    foreach (var lItem in mItems)
                    {
                        if (!lItem.Deleted)
                        {
                            lItem.SnapshotTouchSequenceForSort();
                            lItems.Add(lItem);
                        }
                    }

                    mItems = new List<cTempFileItem>(lItems);
                }

                if (mBackgroundCancellationTokenSource.IsCancellationRequested) return;

                lItems.Sort();

                if (mBackgroundCancellationTokenSource.IsCancellationRequested) return;

                var lSectionCacheItems = GetSectionCacheItems(lContext);

                if (mBackgroundCancellationTokenSource.IsCancellationRequested) return;

                foreach (var lItem in lItems)
                {
                    if (!ZOverBudget()) break;

                    if (lSectionCacheItems.TryGetValue(lItem.GetItemKey(), out var lSectionCacheItem))
                    {
                        lSectionCacheItem.TryDelete(lContext);
                        if (mBackgroundCancellationTokenSource.IsCancellationRequested) return;
                    }
                }
            }
        }





        internal void ItemEncached(cFileBasedSectionCacheItem pItem, long pLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ItemEncached), pItem);

            lock (mLock)
            {
                mAllCachedItems[pItem.ItemKey] = pItem;
                if (pItem.PersistentKey != null) mPersistentKeyItems[pItem.PersistentKey] = pItem;<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< // NO: this isn't pkasgiedn
            }

            Interlocked.Add(ref mByteCount, pLength);
            Interlocked.Increment(ref mFileCount);
        }

        internal void ItemDecached(long pLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ItemDecached), pLength);
            Interlocked.Add(ref mByteCount, -pLength);
            Interlocked.Decrement(ref mByteCount);
        }



        protected void AddFiles(Dictionary<cSectionCachePersistentKey, FileInfo> pFiles)
        {
            lock (mLock)
            {
                foreach (var lPair in pFiles)
                {
                    if (mAllItems.TryGetValue(lPair.Value.FullName, out var lExistingItem) && !lExistingItem.Deleted)
                    {
                        // if it is the same item that I already have, don't do anything
                        if (lExistingItem.PersistentKey == lPair.Key) continue;

                        // this indicates that the file has been re-used
                        lExistingItem.SetDeleted(lcontext);
                    }

                    var lNewItem = zmakeitem();

                    mAllItems[lPair.Value.FullName] = lNewItem;
                    ;?; // sum lengths

                    // if there is an existing item representing this key, the new one now takes over
                    mPersistentKeyItems[lPair.Key] = lNewItem;
                }
            }

            Interlocked.Add(ref mByteCount, lAddedBytes);
            Interlocked.Add(ref mFileCount, lAddedFileCount);

            if (ZOverBudget()) TriggerTidy();
        }

        private bool ZOverBudget() => mFileCount > FileCountBudget || mByteCount > ByteCountBudget;

        public override string ToString() => $"{nameof(cFileBasedSectionCache)}({InstanceName})";

        private class cSnapshot
        {
            public readonly c
        }
    }
}