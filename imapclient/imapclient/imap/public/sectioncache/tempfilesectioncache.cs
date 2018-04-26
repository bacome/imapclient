using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cTempFileSectionCache : cSectionCache
    {
        private static int mTouchSeqSource = 7;

        private readonly object mLock = new object();

        public readonly int FileCountTrigger;
        public readonly long ByteCountTrigger;
        public readonly int WaitAfterTrim;

        private int mFileCount = 0;
        private long mByteCount = 0;

        private Task mTrimTask = null;
        private Task mDelayTask = null;

        public cTempFileSectionCache(int pFileCountTrigger = 1000, long pByteCountTrigger = 100000000, int pWaitAfterTrim = 60000, string pInstanceName = "work.bacome.cTempFileSectionCache", cBatchSizerConfiguration pWriteConfiguration = null) : base(pInstanceName, pWriteConfiguration ?? new cBatchSizerConfiguration(1000, 100000, 1000, 1000), true)
        {
            var lContext = mRootContext.NewObject(nameof(cTempFileSectionCache), pFileCountTrigger, pByteCountTrigger, pWaitAfterTrim);

            if (pFileCountTrigger < 1) throw new ArgumentOutOfRangeException(nameof(pFileCountTrigger));
            if (pByteCountTrigger < 1) throw new ArgumentOutOfRangeException(nameof(pByteCountTrigger));
            if (pWaitAfterTrim < 0) throw new ArgumentOutOfRangeException(nameof(pWaitAfterTrim));

            FileCountTrigger = pFileCountTrigger;
            ByteCountTrigger = pByteCountTrigger;
            WaitAfterTrim = pWaitAfterTrim;
        }

        protected override cItem GetNewItem() => new cTempFileItem(this);

        protected override void ItemAdded(cItem pItem)
        {
            if (!(pItem is cTempFileItem lItem)) throw new ArgumentOutOfRangeException(nameof(pItem));

            lock (mLock)
            {
                mFileCount++;
                mByteCount += lItem.Length;
            }

            ZTrimIfRequired();
        }

        protected override void ItemDeleted(cItem pItem)
        {
            if (!(pItem is cTempFileItem lItem)) throw new ArgumentOutOfRangeException(nameof(pItem));

            lock (mLock)
            {
                mFileCount--;
                mByteCount -= lItem.Length;
            }
        }

        protected override void ItemClosed()
        {
            ZTrimIfRequired();
        }

        private void ZTrimIfRequired()
        {
            lock (mLock)
            {
                if (mFileCount < FileCountTrigger && mByteCount < ByteCountTrigger) return; // not required
                if (mTrimTask != null && !mTrimTask.IsCompleted) return; // currently running
                mTrimTask = ZTrimAsync();
            }
        }

        private async Task ZTrimAsync()
        {
            while (true)
            {
                if (mDelayTask != null) await mDelayTask.ConfigureAwait(false);

                // do stuff
                ;?;

                if (WaitAfterTrim > 0) mDelayTask = Task.Delay(WaitAfterTrim);

                lock (mLock)
                {
                    if (mFileCount < FileCountTrigger && mByteCount < ByteCountTrigger)
                    {
                        mTrimTask = null;
                        return;
                    }
                }
            }
        }

        private class cTempFileItem : cItem
        {
            public int mTouchSeq;
            private readonly string mTempFileName;
            private long? mLength = null;

            public cTempFileItem(cTempFileSectionCache pCache) : base(pCache, true)
            {
                mTouchSeq = Interlocked.Increment(ref mTouchSeqSource);
                mTempFileName = Path.GetTempFileName();
            }

            protected override Stream GetReadStream() => new FileStream(mTempFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            protected override Stream GetReadWriteStream() => new FileStream(mTempFileName, FileMode.Truncate, FileAccess.Write, FileShare.Read);

            protected override void Touch()
            {
                mTouchSeq = Interlocked.Increment(ref mTouchSeqSource);
            }

            protected override void Delete()
            {
                File.Delete(mTempFileName);
            }

            internal long Length
            {
                get
                {
                    if (mLength == null)
                    {
                        var lFileInfo = new FileInfo(mTempFileName);
                        mLength = lFileInfo.Length;
                    }

                    return mLength.Value;
                }
            }
        }
    }
}