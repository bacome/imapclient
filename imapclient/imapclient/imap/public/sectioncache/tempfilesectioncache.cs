using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cTempFileSectionCache : cSectionCache
    {
        public readonly long ByteCountBudget;
        public readonly int FileCountBudget;
        public readonly int Tries;

        private readonly ConcurrentDictionary<string, cItem> mItems = new ConcurrentDictionary<string, cItem>();

        public cTempFileSectionCache(string pInstanceName, int pMaintenanceFrequency, long pByteCountBudget, int pFileCountBudget, int pTries) : base(pInstanceName, pMaintenanceFrequency)
        {
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));
            if (pTries < 1) throw new ArgumentOutOfRangeException(nameof(pTries));

            ByteCountBudget = pByteCountBudget;
            FileCountBudget = pFileCountBudget;
            Tries = pTries;

            StartMaintenance();
        }

        protected override cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(YGetNewItem));

            int lTries = 0;
            string lFullName;
            Stream lStream;

            while (true)
            {
                try
                {
                    lFullName = Path.GetTempFileName();
                    lStream = new FileStream(lFullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                    break;
                }
                catch (IOException) { }

                if (++lTries == Tries) throw new IOException();
            }

            var lFileInfo = new FileInfo(lFullName);
            var lItem = new cItem(this, lFullName, lStream, lFileInfo.CreationTimeUtc);
            mItems[lFullName] = lItem;

            return lItem;
        }

        protected override void Maintenance(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTempFileSectionCache), nameof(Maintenance));

            int lSnapshotSequence = GetItemSequence();

            List<cItemSnapshot> lItemSnapshots = new List<cItemSnapshot>();

            long lByteCount = 0;

            foreach (var lPair in mItems)
            {
                var lItem = lPair.Value;

                if (lItem.ItemSequence < lSnapshotSequence && !lItem.Deleted)
                {
                    var lItemSnapshot = new cItemSnapshot(lItem);
                    lItemSnapshots.Add(lItemSnapshot);
                    lByteCount += lItemSnapshot.Length;
                }
            }

            if (lItemSnapshots.Count <= FileCountBudget && lByteCount <= ByteCountBudget) return;

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            lItemSnapshots.Sort();

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            int lFileCount = lItemSnapshots.Count;

            foreach (var lItemSnapshot in lItemSnapshots)
            {
                if (lItemSnapshot.TryDelete(lContext))
                {
                    lFileCount--;
                    lByteCount -= lItemSnapshot.Length;
                    if (lFileCount <= FileCountBudget && lByteCount <= ByteCountBudget) return;
                }

                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
            }
        }

        private class cItem : cSectionCacheItem
        {
            private static int mTouchSequenceSource = 7;

            private DateTime mCreationTimeUTC;
            private int mTouchSequence;

            public cItem(cTempFileSectionCache pCache, string pFullName, Stream pReadWriteStream, DateTime pCreationTimeUTC) : base(pCache, pFullName, pReadWriteStream)
            {
                mCreationTimeUTC = pCreationTimeUTC;
                mTouchSequence = Interlocked.Increment(ref mTouchSequenceSource);
            }

            protected override Stream YGetReadStream(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(YGetReadStream));
                var lStream = new FileStream(ItemKey, FileMode.Open, FileAccess.Read, FileShare.Read);
                var lFileInfo = new FileInfo(ItemKey);
                if (FileTimesAreTheSame(lFileInfo.CreationTimeUtc, mCreationTimeUTC)) return lStream; // length is checked by the cache
                lStream.Dispose();
                return null;
            }

            protected override void YDelete(cTrace.cContext pParentContext) => File.Delete(ItemKey);

            protected override void Touch(cTrace.cContext pParentContext) => Interlocked.Increment(ref mTouchSequenceSource);

            public int TouchSequence => mTouchSequence;
        }

        private class cItemSnapshot : IComparable<cItemSnapshot>
        {
            private readonly cItem mItem;
            private readonly int mChangeSequence;
            private readonly int mTouchSequence;
            public readonly long Length;

            public cItemSnapshot(cItem pItem)
            {
                mItem = pItem;
                mChangeSequence = pItem.ChangeSequence;
                mTouchSequence = pItem.TouchSequence;
                Length = pItem.Length;
            }

            public bool TryDelete(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItemSnapshot), nameof(TryDelete));
                return mItem.TryDelete(mChangeSequence, lContext);
            }

            public int CompareTo(cItemSnapshot pOther)
            {
                if (pOther == null) return 1;
                return mTouchSequence.CompareTo(pOther.mTouchSequence);
            }
        }
    }
}