using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFileBasedSectionCache : cSectionCache
    {
        public readonly long ByteCountBudget;
        public readonly int FileCountBudget;

        private readonly Dictionary<object, cFileBasedSectionCacheItem> mAllItems = new Dictionary<object, cFileBasedSectionCacheItem>();
        private readonly Dictionary<cSectionCachePersistentKey, cFileBasedSectionCacheItem> mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cFileBasedSectionCacheItem>();
        private readonly object mLock = new object();

        private long mByteCount = 0;
        private int mFileCount = 0;

        public cFileBasedSectionCache(string pInstanceName, int pDelayAfterTidy, long pByteCountBudget, int pFileCountBudget) : base(pInstanceName, pDelayAfterTidy)
        {
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));

            FileCountBudget = pFileCountBudget;
            ByteCountBudget = pByteCountBudget;

            if (pExistingItems == null)
            {
                mWorthTryingTrim = false;
            }
            else
            {
                foreach (var lPair in pExistingItems)
                {
                    if (lPair.Value == null) throw new ArgumentOutOfRangeException(nameof(pExistingItems), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                    var lItem = new cItem(this, lPair.Value);
                    mPersistentKeyItems.Add(lPair.Key, lItem);
                    mAllItems.Add(lItem.GetItemKey(), lItem);
                    mByteCount += lItem.AccountingLength;
                    mFileCount++;
                }

                mWorthTryingTrim = true;
            }
        }

        protected abstract string GetNewFileName(cTrace.cContext pParentContext);

        sealed protected override cSectionCacheItem GetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(GetNewItem));

            while (true)
            {
                string lFileName = GetNewFileName(lContext);

                var lStream = new FileStream(lFileName, FileMode.Truncate, FileAccess.ReadWrite, FileShare.Read);

                cFileBasedSectionCacheItem lItem;

                try
                {
                    lock (mLock)
                    {
                        if (mAllItems.TryGetValue(lFileName, out lItem) && !lItem.Deleted) lContext.TraceWarning("allocated an in-use file name {0}", lFileName);
                        else
                        {
                            ;?;
                            // need  a 
                            lItem = new cFileBasedSectionCacheItem(this, lStream, lFileName);
                            mAllItems.Add(lFileName, lItem);
                            return lItem;
                        }
                    }
                }
                catch
                {
                    lStream.Dispose();
                    throw;
                }

                lStream.Dispose();
                lItem.SetDeleted(lContext);
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

        // permanent cache should override this to write the key -> file mapping and mark the item as permanentkeyassigned
        protected internal override void ItemAdded(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ItemAdded), pItem);

            if (!(pItem is cFileBasedSectionCacheItem lNewItem)) throw new cInternalErrorException(nameof(cFileBasedSectionCache), nameof(ItemAdded));

            lock (mLock)
            {
                // if the file points to an existing item, mark that item as deleted
                if (mAllItems.TryGetValue(lNewItem.filename, out var lExistingItem) && !lExistingItem.Deleted) lExistingItem.SetDeleted(lContext);

                // store
                mAllItems[lNewItem.Filename] = lNewItem;

                // if there is an existing item representing this key, the new one now takes over
                if (lNewItem.PersistentKey != null) mPersistentKeyItems[lNewItem.PersistentKey] = lNewItem;
            }

            Interlocked.Add(ref mByteCount, lNewItem.AccountingLength);
            Interlocked.Increment(ref mFileCount);
        }

        sealed protected internal override void ItemDeleted(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ItemDeleted), pItem);

            if (!(pItem is cFileBasedSectionCacheItem lItem)) throw new cInternalErrorException(nameof(cFileBasedSectionCache), nameof(ItemDeleted));

            Interlocked.Add(ref mByteCount, -lItem.AccountingLength);
            Interlocked.Decrement(ref mByteCount);
        }

        protected override void Tidy(Dictionary<object, cSectionCacheItemSnapshot> pSnapshots, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(Tidy));

            if (ZOverBudget())
            {
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
    }
}