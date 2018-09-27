using System;
using System.Runtime.Serialization;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderCacheItem : cPersistentCacheItem
    {
        protected internal cHeaderCacheItem(cHeaderCache pCache, cHeaderCacheItemData pData) : base(pCache, pData) { }

        public fMessageCacheAttributes Attributes => ((cHeaderCacheItemData)mData).Attributes;

        public cEnvelope Envelope
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).Envelope;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }

            internal set => ((cHeaderCacheItemData)mData).Envelope = value;
        }

        public DateTimeOffset? ReceivedDateTimeOffset
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).ReceivedDateTimeOffset;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }
        }

        public DateTime? ReceivedDateTime
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).ReceivedDateTime;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }
        }

        internal void SetReceivedDateTime(DateTimeOffset pOffset, DateTime pDateTime) => ((cHeaderCacheItemData)mData).SetReceivedDateTime(pOffset, pDateTime);

        public uint? Size
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).Size;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }
        }

        internal void SetSize(uint pSize) => ((cHeaderCacheItemData)mData).SetSize(pSize);

        public cBodyPart BodyStructure
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).BodyStructure;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }

            internal set => ((cHeaderCacheItemData)mData).BodyStructure = value;
        }

        public cHeaderFields HeaderFields
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).HeaderFields;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }
        }

        internal void AddHeaderFields(cHeaderFields pHeaderFields) => ((cHeaderCacheItemData)mData).AddHeaderFields(pHeaderFields);

        public cBinarySizes BinarySizes
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).BinarySizes;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }
        }

        internal void AddBinarySizes(cBinarySizes pBinarySizes) => ((cHeaderCacheItemData)mData).AddBinarySizes(pBinarySizes);
    }

    [Serializable]
    public class cHeaderCacheItemData : cPersistentCacheItemData
    {
        [NonSerialized]
        private fMessageCacheAttributes mAttributes;

        private cEnvelope mEnvelope;

        private DateTimeOffset? mReceivedDateTimeOffset;
        private DateTime? mReceivedDateTime;
        private uint? mSize;

        ;...;
        private cBodyPart mBodyStructure;
        private cHeaderFields mHeaderFields;
        private cBinarySizes mBinarySizes;

        internal cHeaderCacheItemData()
        {
            mAttributes = 0;
            mEnvelope = null;
            mReceivedDateTimeOffset = null;
            mReceivedDateTime = null;
            mSize = null;
            mBodyStructure = null;
            mHeaderFields = null;
            mBinarySizes = null;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mEnvelope != null) mAttributes |= fMessageCacheAttributes.envelope;

            if (mReceivedDateTimeOffset == null)
            {
                if (mReceivedDateTime != null) throw new cDeserialiseException(nameof(cHeaderCacheItemData), nameof(mReceivedDateTime), kDeserialiseExceptionMessage.IsInconsistent);
            }
            else
            {
                if (mReceivedDateTime == null) throw new cDeserialiseException(nameof(cHeaderCacheItemData), nameof(mReceivedDateTime), kDeserialiseExceptionMessage.IsInconsistent, 2);
                if (!ZDatesAreConsistent(mReceivedDateTimeOffset.Value, mReceivedDateTime.Value)) throw new cDeserialiseException(nameof(cHeaderCacheItemData), nameof(mReceivedDateTime), kDeserialiseExceptionMessage.IsInconsistent, 3);
                mAttributes |= fMessageCacheAttributes.received;
            }

            if (mSize != null) mAttributes |= fMessageCacheAttributes.size;
            if (mBodyStructure != null) mAttributes |= fMessageCacheAttributes.bodystructure;
        }

        public fMessageCacheAttributes Attributes => mAttributes;

        public cEnvelope Envelope
        {
            get => mEnvelope;

            internal set
            {
                if (value == null) throw new ArgumentNullException();

                lock (mUpdateLock)
                {
                    if (mEnvelope != null) return;
                    mEnvelope = value;
                    mAttributes |= fMessageCacheAttributes.envelope;
                }
            }
        }

        public DateTimeOffset? ReceivedDateTimeOffset => mReceivedDateTimeOffset;
        public DateTime? ReceivedDateTime => mReceivedDateTime;

        internal void SetReceivedDateTime(DateTimeOffset pOffset, DateTime pDateTime)
        {
            if (!ZDatesAreConsistent(pOffset, pDateTime)) throw new ArgumentOutOfRangeException(nameof(pDateTime));

            lock (mUpdateLock)
            {
                if (mReceivedDateTimeOffset != null) return;
                mReceivedDateTimeOffset = pOffset;
                mReceivedDateTime = pDateTime;
                mAttributes |= fMessageCacheAttributes.received;
            }
        }

        public uint? Size => mSize;

        internal void SetSize(uint pSize)
        {
            lock (mUpdateLock)
            {
                if (mSize != null) return;
                mSize = pSize;
                mAttributes |= fMessageCacheAttributes.size;
            }
        }

        public cBodyPart BodyStructure
        {
            get => mBodyStructure;

            internal set
            {
                if (value == null) throw new ArgumentNullException();

                lock (mUpdateLock)
                {
                    if (mBodyStructure != null) return;
                    mBodyStructure = value;
                    mAttributes |= fMessageCacheAttributes.bodystructure;
                }
            }
        }

        public cHeaderFields HeaderFields => mHeaderFields;

        internal void AddHeaderFields(cHeaderFields pHeaderFields)
        {
            lock (mUpdateLock)
            {
                mHeaderFields += pHeaderFields;
            }
        }

        public cBinarySizes BinarySizes => mBinarySizes;

        internal void AddBinarySizes(cBinarySizes pBinarySizes)
        {
            lock (mUpdateLock)
            {
                mBinarySizes += pBinarySizes;
            }
        }

        private static bool ZDatesAreConsistent(DateTimeOffset pOffset, DateTime pDateTime)
        {
            if (pDateTime.Kind == DateTimeKind.Local && pDateTime == pOffset.LocalDateTime) return true;
            if (pDateTime.Kind == DateTimeKind.Unspecified && pDateTime == pOffset.DateTime) return true;
            return false;
        }

        public override string ToString() => $"{nameof(cHeaderCacheItemData)}({base.ToString()},{mAttributes},{Envelope},{mReceivedDateTimeOffset},{mReceivedDateTime},{mSize},{BodyStructure},{HeaderFields},{mBinarySizes})";
    }
}
