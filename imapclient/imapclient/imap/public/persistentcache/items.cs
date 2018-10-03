using System;

namespace work.bacome.imapclient
{







    public abstract class cPersistentCacheItemx
    {
        protected readonly cPersistentCacheComponent mCache;
        protected readonly cPersistentCacheItemData mData;

        protected cPersistentCacheItem(cPersistentCacheComponent pCache, cPersistentCacheItemData pData)
        {
            mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            mData = pData ?? throw new ArgumentNullException(nameof(pData));
        }

        public DateTime AccessDateTime => mData.AccessDateTime;
        public long AccessSequenceNumber => mData.AccessSequenceNumber;

        internal void RecordAccess() => mData.SetAccessed(mCache.GetNextAccessSequenceNumber());

        public override string ToString() => $"{nameof(cPersistentCacheItem)}({mCache},{mData})";
    }

    [Serializable]
    public abstract class cPersistentCacheItemDatax
    {
        [NonSerialized]
        protected readonly object mUpdateLock = new object();

        private DateTime mAccessDateTime;
        private long mAccessSequenceNumber;

        protected cPersistentCacheItemData()
        {
            mAccessDateTime = DateTime.MinValue;
            mAccessSequenceNumber = long.MinValue;
        }

        public DateTime AccessDateTime => mAccessDateTime;
        public long AccessSequenceNumber => mAccessSequenceNumber;

        internal void SetAccessed(long pAccessSequenceNumber)
        {
            lock (mUpdateLock)
            {
                mAccessDateTime = DateTime.UtcNow;
                if (pAccessSequenceNumber > mAccessSequenceNumber) mAccessSequenceNumber = pAccessSequenceNumber;
            }
        }

        public override string ToString() => $"{nameof(cPersistentCacheItemData)}({mAccessDateTime},{mAccessSequenceNumber})";
    }
}