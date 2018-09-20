using System;

namespace work.bacome.imapclient
{
    public abstract class cPersistentCacheItem
    {
        protected readonly cPersistentCacheComponent mCache;

        private long mAccessSequenceNumber;
        private DateTime mAccessDateTime;

        protected cPersistentCacheItem(cPersistentCacheComponent pCache, long pAccessSequenceNumber, DateTime pAccessDateTime)
        {
            pCache = mCache ?? throw new ArgumentNullException(nameof(pCache));
            if (pAccessSequenceNumber < 0) throw new ArgumentOutOfRangeException(nameof(pAccessSequenceNumber));
            mAccessSequenceNumber = pAccessSequenceNumber;
            mAccessDateTime = pAccessDateTime;
        }

        protected void RecordAccess()
        {
            mAccessSequenceNumber = mCache.GetNextAccessSequenceNumber();
            mAccessDateTime = DateTime.UtcNow;
        }
    }
}