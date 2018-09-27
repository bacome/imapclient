using System;

namespace work.bacome.imapclient
{
    public abstract class cFlagCacheItem : cPersistentCacheItem
    {
        protected internal cFlagCacheItem(cFlagCache pCache, cFlagCacheItemData pData) : base(pCache, pData) { }

        public fMessageCacheAttributes Attributes => ((cFlagCacheItemData)mData).Attributes;

        public cModSeqFlags ModSeqFlags
        {
            get
            {
                var lValue = ((cFlagCacheItemData)mData).ModSeqFlags;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }

            internal set => ((cFlagCacheItemData)mData).ModSeqFlags = value;
        }
    }

    [Serializable]
    public class cFlagCacheItemData : cPersistentCacheItemData
    {
        private cModSeqFlags mModSeqFlags;

        internal cFlagCacheItemData()
        {
            mModSeqFlags = null;
        }

        public fMessageCacheAttributes Attributes => mModSeqFlags == null ? 0 : fMessageCacheAttributes.flags;

        public cModSeqFlags ModSeqFlags
        {
            get => mModSeqFlags;

            internal set
            {
                if (value == null) throw new ArgumentNullException();

                lock (mUpdateLock)
                {
                    if (mModSeqFlags != null && value.ModSeq != 0 && value.ModSeq <= mModSeqFlags.ModSeq) return;
                    mModSeqFlags = value;
                }
            }
        }
    }
}
