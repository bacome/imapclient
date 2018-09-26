using System;

namespace work.bacome.imapclient
{
    public abstract class cPersistentCacheItem
    {
        protected readonly cPersistentCacheComponent mCache;
        protected readonly cPersistentCacheItemData mData;

        protected cPersistentCacheItem(cPersistentCacheComponent pCache, cPersistentCacheItemData pData)
        {
            pCache = mCache ?? throw new ArgumentNullException(nameof(pCache));
            mData = pData ?? throw new ArgumentNullException(nameof(pData));
        }

        public long AccessSequenceNumber => mData.AccessSequenceNumber;
        public DateTime AccessDateTime => mData.AccessDateTime;

        public override string ToString() => $"{nameof(cPersistentCacheItem)}({mCache},{mData})";
    }

    [Serializable]
    public abstract class cPersistentCacheItemData
    {
        private long mAccessSequenceNumber;
        private DateTime mAccessDateTime;

        protected cPersistentCacheItemData(cPersistentCacheComponent pCache)
        {
            RecordAccess(pCache);
        }

        public long AccessSequenceNumber => mAccessSequenceNumber;
        public DateTime AccessDateTime => mAccessDateTime;

        protected void RecordAccess(cPersistentCacheComponent pCache)
        {
            mAccessSequenceNumber = pCache.GetNextAccessSequenceNumber();
            mAccessDateTime = DateTime.UtcNow;
        }

        public override string ToString() => $"{nameof(cPersistentCacheItemData)}({mAccessSequenceNumber},{mAccessDateTime})";
    }
}