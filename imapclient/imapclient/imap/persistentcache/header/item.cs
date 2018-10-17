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

        public cTimestamp Received
        {
            get
            {
                var lValue = ((cHeaderCacheItemData)mData).Received;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }

            internal set => ((cHeaderCacheItemData)mData).Received = value;
        }

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
        private cTimestamp mReceived;
        private uint? mSize;
        private cBodyPart mBodyStructure;
        private cHeaderFields mHeaderFields;
        private cBinarySizes mBinarySizes;

        internal cHeaderCacheItemData()
        {
            mAttributes = 0;
            mEnvelope = null;
            mReceived = null;
            mSize = null;
            mBodyStructure = null;
            mHeaderFields = null;
            mBinarySizes = null;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mEnvelope != null) mAttributes |= fMessageCacheAttributes.envelope;
            if (mReceived != null) mAttributes |= fMessageCacheAttributes.received;
            if (mSize != null) mAttributes |= fMessageCacheAttributes.size;

            if (mBodyStructure != null)
            {
                ;?; // 
                if (mBodyStructure.Section != cSection.Text) throw new cDeserialiseException(nameof(cHeaderCacheItemData), nameof(mBodyStructure), kDeserialiseExceptionMessage.IsInvalid);
                mAttributes |= fMessageCacheAttributes.bodystructure;
            }
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

        public cTimestamp Received
        {
            get => mReceived;

            internal set
            {
                if (value == null) throw new ArgumentNullException();

                lock (mUpdateLock)
                {
                    if (mReceived != null) return;
                    mReceived = value;
                    mAttributes |= fMessageCacheAttributes.received;
                }
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

        public override string ToString() => $"{nameof(cHeaderCacheItemData)}({base.ToString()},{mAttributes},{Envelope},{mReceived},{mSize},{BodyStructure},{HeaderFields},{mBinarySizes})";
    }
}
