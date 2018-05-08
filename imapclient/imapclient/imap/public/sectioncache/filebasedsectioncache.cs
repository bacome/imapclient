using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFileBasedSectionCache : cSectionCache, IDisposable
    {
        public readonly string InstanceName;
        public readonly long ByteCountBudget;
        public readonly int FileCountBudget;
        public readonly int WaitAfterTrim;

        ;?; // this gets added to as keys are assigned ... if there is an entry in here when the key is assigned then the filename should be changed to point at the one here
        ;?; // if a sub class has a way of finding items ... ;?;
        private readonly Dictionary<cSectionCachePersistentKey, cFileBasedSectionCacheItem> mPersistentKeyItems = new Dictionary<cSectionCachePersistentKey, cFileBasedSectionCacheItem>();
        private readonly Dictionary<object, cFileBasedSectionCacheItem> mAllItems = new Dictionary<object, cFileBasedSectionCacheItem>();
        private readonly object mLock = new object();

        private long mByteCount = 0;
        private int mFileCount = 0;
        private bool mWorthTryingTrim;

        public cFileBasedSectionCache(string pInstanceName, long pByteCountBudget, int pFileCountBudget, int pWaitAfterTrim, Dictionary<cSectionCachePersistentKey, FileInfo> pExistingItems) : base(pExistingItems == null)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));

            var lContext = cMailClient.Trace.NewRoot(pInstanceName);

            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pWaitAfterTrim < 0) throw new ArgumentOutOfRangeException(nameof(pWaitAfterTrim));

            FileCountBudget = pFileCountBudget;
            ByteCountBudget = pByteCountBudget;
            WaitAfterTrim = pWaitAfterTrim;

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




            mBackgroundReleaser = new cReleaser(pInstanceName, mBackgroundCancellationTokenSource.Token);
            mBackgroundTask = ZBackgroundTaskAsync(lContext);
        }

        protected abstract string GetNewFileName(cTrace.cContext pParentContext);

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
                            lItem = new cFileBasedSectionCacheItem(this, lFileName, lStream);
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
                lItem.TryDelete(-1, lContext);
            }
        }

        protected internal override void ItemAdded(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ItemAdded), pItem);

            if (!(pItem is cFileBasedSectionCacheItem lItem)) throw new cInternalErrorException(nameof(cFileBasedSectionCache), nameof(ItemAdded));

            lock (mLock)
            {
                mAllItems.Add(lItem.ItemKey, lItem);
            }

            ;?; // add to internal lists

            Interlocked.Add(ref mByteCount, lItem.AccountingLength);
            Interlocked.Increment(ref mFileCount);

            ZTriggerTrim(lContext);
        }

        sealed protected internal override void ItemDeleted(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ItemDeleted), pItem);

            if (!(pItem is cFileBasedSectionCacheItem lItem)) throw new cInternalErrorException(nameof(cFileBasedSectionCache), nameof(ItemDeleted));

            ;?;

            Interlocked.Add(ref mByteCount, -lItem.AccountingLength);
            Interlocked.Decrement(ref mByteCount);
        }

        protected internal override void ItemClosed(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ItemClosed));
            mWorthTryingTrim = true;
            ZTriggerTrim(lContext);
        }

        private void ZTriggerTrim(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCache), nameof(ZTriggerTrim));

            if (!ZOverBudget() || !mWorthTryingTrim) return;

            mWorthTryingTrim = false;
            mBackgroundReleaser.Release(lContext);
        }

        private bool ZOverBudget() => mFileCount > FileCountBudget || mByteCount > ByteCountBudget;

        private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
        {
            ;?; // also delete items where the message is invalid

            var lContext = pParentContext.NewRootMethod(nameof(cFileBasedSectionCache), nameof(ZBackgroundTaskAsync));

            try
            {
                while (true)
                {
                    lContext.TraceVerbose("waiting for release");
                    await mBackgroundReleaser.GetAwaitReleaseTask(lContext).ConfigureAwait(false);

                    mBackgroundReleaser.Reset(lContext);

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

                    if (WaitAfterTrim > 0)
                    {
                        lContext.TraceVerbose("sleeping");
                        await Task.Delay(WaitAfterTrim, mBackgroundCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e) when (!mBackgroundCancellationTokenSource.IsCancellationRequested && lContext.TraceException("the section cache background task is stopping due to an unexpected error", e)) { }
        }


        public override string ToString() => $"{nameof(cFileBasedSectionCache)}({InstanceName})";

        public void Dispose()
        {
            if (mDisposed) return;

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

            mDisposed = true;
        }
    }
}