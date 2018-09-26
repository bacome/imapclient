using System;
using System.Runtime.Serialization;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cStaticHeaderCacheItem : cPersistentCacheItem
    {
        protected internal cStaticHeaderCacheItem(cStaticHeaderCache pCache, cStaticHeaderCacheItemData pData) : base(pCache, pData) { }

        public fMessageCacheAttributes Attributes => ((cStaticHeaderCacheItemData)mData).Attributes;

        public DateTimeOffset? ReceivedDateTimeOffset => ((cStaticHeaderCacheItemData)mData).GetReceivedDateTimeOffset(mCache);
        public DateTime? ReceivedDateTime => ((cStaticHeaderCacheItemData)mData).GetReceivedDateTime(mCache);
        internal void SetReceivedDateTime(DateTimeOffset pOffset, DateTime pDateTime) => ((cStaticHeaderCacheItemData)mData).SetReceivedDateTime(pOffset, pDateTime);

        public uint? Size => ((cStaticHeaderCacheItemData)mData).GetSize(mCache);
        internal void SetSize(uint pSize) => ((cStaticHeaderCacheItemData)mData).SetSize(pSize);

        public uint? GetBinarySize(string pPart) => ((cStaticHeaderCacheItemData)mData).GetBinarySize(pPart, mCache);
        internal void AddBinarySizes(cBinarySizes pSizes) => ((cStaticHeaderCacheItemData)mData).AddBinarySizes(pSizes);
    }

    [Serializable]
    public class cStaticHeaderCacheItemData : cPersistentCacheItemData
    {
        [NonSerialized]
        private fMessageCacheAttributes mAttributes;

        private DateTimeOffset? mReceivedDateTimeOffset;
        private DateTime? mReceivedDateTime;
        private uint? mSize;

        [NonSerialized]
        private readonly object mBinarySizesLock = new object();

        private cBinarySizes mBinarySizes;

        internal cStaticHeaderCacheItemData(cStaticHeaderCache pCache) : base(pCache)
        {
            mAttributes = 0;
            mReceivedDateTimeOffset = null;
            mReceivedDateTime = null;
            mSize = null;
            mBinarySizes = null;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mReceivedDateTimeOffset == null)
            {
                if (mReceivedDateTime != null) throw new cDeserialiseException(nameof(cStaticHeaderCacheItemData), nameof(mReceivedDateTime), kDeserialiseExceptionMessage.IsInconsistent);
            }
            else
            {
                if (mReceivedDateTime == null) throw new cDeserialiseException(nameof(cStaticHeaderCacheItemData), nameof(mReceivedDateTime), kDeserialiseExceptionMessage.IsInconsistent, 2);
                if (!ZDatesAreConsistent(mReceivedDateTimeOffset.Value, mReceivedDateTime.Value)) throw new cDeserialiseException(nameof(cStaticHeaderCacheItemData), nameof(mReceivedDateTime), kDeserialiseExceptionMessage.IsInconsistent, 3);

                mAttributes |= fMessageCacheAttributes.received;
            }

            if (mSize != null) mAttributes |= fMessageCacheAttributes.size;
        }

        public fMessageCacheAttributes Attributes => mAttributes;

        public DateTimeOffset? GetReceivedDateTimeOffset(cPersistentCacheComponent pCache)
        {
            RecordAccess(pCache);
            return mReceivedDateTimeOffset;
        }

        public DateTime? GetReceivedDateTime(cPersistentCacheComponent pCache)
        {
            RecordAccess(pCache);
            return mReceivedDateTime;
        }

        internal void SetReceivedDateTime(DateTimeOffset pOffset, DateTime pDateTime)
        {
            if (!ZDatesAreConsistent(pOffset, pDateTime)) throw new ArgumentOutOfRangeException(nameof(pDateTime));
            mReceivedDateTimeOffset = pOffset;
            mReceivedDateTime = pDateTime;
            mAttributes |= fMessageCacheAttributes.received;
        }

        public uint? GetSize(cPersistentCacheComponent pCache)
        {
            RecordAccess(pCache);
            return mSize;
        }

        internal void SetSize(uint pSize)
        {
            mSize = pSize;
            mAttributes |= fMessageCacheAttributes.size;
        }

        public uint? GetBinarySize(string pPart, cPersistentCacheComponent pCache)
        {
            RecordAccess(pCache);
            if (mBinarySizes.TryGetValue(pPart, out var lSize)) return lSize;
            return null;
        }

        internal void AddBinarySizes(cBinarySizes pSizes)
        {
            lock (mBinarySizesLock)
            {
                mBinarySizes = mBinarySizes + pSizes;
            }
        }

        private static bool ZDatesAreConsistent(DateTimeOffset pOffset, DateTime pDateTime)
        {
            if (pDateTime.Kind == DateTimeKind.Local && pDateTime == pOffset.LocalDateTime) return true;
            if (pDateTime.Kind == DateTimeKind.Unspecified && pDateTime == pOffset.DateTime) return true;
            return false;
        }

        public override string ToString() => $"{nameof(cStaticHeaderCacheItemData)}({mAttributes},{mReceivedDateTimeOffset},{mReceivedDateTime},{mSize},{mBinarySizes})";
    }
}
