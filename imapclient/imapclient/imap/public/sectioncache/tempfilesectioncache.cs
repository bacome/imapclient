using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public sealed class cTempFileSectionCache : cSectionCache, IDisposable
    {
        private static int mTouchSequenceSource = 7;

        private bool mDisposed = false;

        private readonly object mLock = new object();

        public readonly string InstanceName;
        public readonly int FileCountBudget;
        public readonly long ByteCountBudget;
        public readonly int WaitAfterTrim;

        private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource();
        private readonly cReleaser mBackgroundReleaser;
        private readonly Task mBackgroundTask = null;

        private List<cTempFileItem> mItems = new List<cTempFileItem>();
        private int mFileCount = 0;
        private long mByteCount = 0;
        private bool mWorthTryingTrim = false;

        public cTempFileSectionCache(string pInstanceName, int pFileCountBudget, long pByteCountBudget, int pWaitAfterTrim) : base(true)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));

            var lContext = cMailClient.Trace.NewRoot(pInstanceName);

            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pWaitAfterTrim < 0) throw new ArgumentOutOfRangeException(nameof(pWaitAfterTrim));

            FileCountBudget = pFileCountBudget;
            ByteCountBudget = pByteCountBudget;
            WaitAfterTrim = pWaitAfterTrim;

            mBackgroundReleaser = new cReleaser(pInstanceName, mBackgroundCancellationTokenSource.Token);
            mBackgroundTask = ZBackgroundTaskAsync(lContext);
        }

        protected override cItem GetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(GetNewItem));
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            return new cTempFileItem(this);
        }

        protected override void ItemAdded(cItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(ItemAdded), pItem);

            if (!(pItem is cTempFileItem lItem)) throw new cInternalErrorException(nameof(cTempFileSectionCache), nameof(ItemAdded));

            lock (mLock)
            {
                mItems.Add(lItem);
                mFileCount++;
                mByteCount += pItem.Length;
            }

            ZTriggerTrim(lContext);
        }

        protected override void ItemDeleted(cItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(ItemDeleted), pItem);

            if (!(pItem is cTempFileItem lItem)) throw new ArgumentOutOfRangeException(nameof(pItem));

            lock (mLock)
            {
                mFileCount--;
                mByteCount -= lItem.Length;
            }
        }

        protected override void ItemClosed(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(ItemClosed));
            mWorthTryingTrim = true;
            ZTriggerTrim(lContext);
        }

        private void ZTriggerTrim(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(ZTriggerTrim));

            if (!ZOverBudget() || !mWorthTryingTrim) return;

            mWorthTryingTrim = false;
            mBackgroundReleaser.Release(lContext);
        }

        private bool ZOverBudget() => mFileCount > FileCountBudget || mByteCount > ByteCountBudget;

        private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cTempFileSectionCache), nameof(ZBackgroundTaskAsync));

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

                            if (lSectionCacheItems.TryGetValue(lItem.FileName, out var lSectionCacheItem))
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

        public override string ToString() => $"{nameof(cTempFileSectionCache)}({InstanceName})";

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

        private class cTempFileItem : cItem, IComparable<cTempFileItem>
        {
            public readonly string FileName;
            private int mTouchSequence;
            private int mSnapshotTouchSequence;

            public cTempFileItem(cTempFileSectionCache pCache) : base(pCache)
            {
                FileName = Path.GetTempFileName();
                mTouchSequence = Interlocked.Increment(ref mTouchSequenceSource);
                mSnapshotTouchSequence = mTouchSequence;
            }

            protected override Stream GetReadStream(cTrace.cContext pParentContext) => new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            protected override Stream GetReadWriteStream(cTrace.cContext pParentContext) => new FileStream(FileName, FileMode.Truncate, FileAccess.Write, FileShare.Read);

            protected override void Touch(cTrace.cContext pParentContext)
            {
                mTouchSequence = Interlocked.Increment(ref mTouchSequenceSource);
            }

            protected override void Delete(cTrace.cContext pParentContext) => File.Delete(FileName);

            protected internal override object GetItemKey() => FileName;

            public void SnapshotTouchSequenceForSort()
            {
                mSnapshotTouchSequence = mTouchSequence;
            }

            public int CompareTo(cTempFileItem pOther)
            {
                if (pOther == null) return 1;
                return mSnapshotTouchSequence.CompareTo(pOther.mSnapshotTouchSequence);
            }

            public override string ToString() => $"{nameof(cTempFileItem)}({FileName},{mTouchSequence})";
        }
    }
}