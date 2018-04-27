using System;
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

        private int mFileCount = 0;
        private long mByteCount = 0;
        private bool mWorthTryingTrim = false;

        public cTempFileSectionCache(string pInstanceName, int pFileCountBudget, long pByteCountBudget, int pWaitAfterTrim, cBatchSizerConfiguration pWriteConfiguration) : base(pWriteConfiguration, true)
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

        protected override cItem GetNewItem(cTrace.cContext pParentContext) => new cTempFileItem(this);

        protected override void ItemAdded(cItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(ItemAdded), pItem);

            if (!(pItem is cTempFileItem lItem)) throw new ArgumentOutOfRangeException(nameof(pItem));

            lock (mLock)
            {
                mFileCount++;
                mByteCount += lItem.ByteCount;
            }

            ZTriggerTrim(lContext);
        }

        protected override void ItemDeleted(cItem pItem, cTrace.cContext pParentContext)
        {
            if (!(pItem is cTempFileItem lItem)) throw new ArgumentOutOfRangeException(nameof(pItem));

            lock (mLock)
            {
                mFileCount--;
                mByteCount -= lItem.ByteCount;
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

            while (true)
            {
                await mBackgroundReleaser.GetAwaitReleaseTask(lContext).ConfigureAwait(false);
                mBackgroundReleaser.Reset(lContext);

                var lItems = GetTrimItems(lContext);

                foreach (var lItem in lItems)
                {
                    if (mBackgroundCancellationTokenSource.IsCancellationRequested) return;
                    lItem.TryDelete(lContext);
                    if (!ZOverBudget()) break;
                }

                if (WaitAfterTrim > 0) await Task.Delay(WaitAfterTrim, mBackgroundCancellationTokenSource.Token).ConfigureAwait(false);
            }
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

        private class cTempFileItem : cItem
        {
            private readonly string mTempFileName;
            private int mTouchSequence;
            private long mByteCount = -1;

            public cTempFileItem(cTempFileSectionCache pCache) : base(pCache, true)
            {
                mTempFileName = Path.GetTempFileName();
                mTouchSequence = Interlocked.Increment(ref mTouchSequenceSource);
            }

            protected override Stream GetReadStream(cTrace.cContext pParentContext) => new FileStream(mTempFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            protected override Stream GetReadWriteStream(cTrace.cContext pParentContext) => new FileStream(mTempFileName, FileMode.Truncate, FileAccess.Write, FileShare.Read);

            protected override void Touch(cTrace.cContext pParentContext)
            {
                mTouchSequence = Interlocked.Increment(ref mTouchSequenceSource);
            }

            protected override void Delete(cTrace.cContext pParentContext) => File.Delete(mTempFileName);

            protected internal override IComparable GetSortParameters() => mTouchSequence;

            internal long ByteCount
            {
                get
                {
                    if (mByteCount == -1)
                    {
                        var lFileInfo = new FileInfo(mTempFileName);
                        mByteCount = lFileInfo.Length;
                    }

                    return mByteCount;
                }
            }

            public override string ToString() => $"{nameof(cTempFileItem)}({mTempFileName},{mTouchSequence},{mByteCount})";
        }
    }
}