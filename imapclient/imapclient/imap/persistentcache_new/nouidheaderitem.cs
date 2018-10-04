using System;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal class cNoUIDHeaderItem : iHeaderItem
    {
        private fMessageCacheAttributes mAttributes = 0;
        private cEnvelope mEnvelope = null;
        private cTimestamp mReceived = null;
        private uint? mSize = null;
        private cBodyPart mBodyStructure = null;
        private cHeaderFields mHeaderFields = null;
        private cBinarySizes mBinarySizes = null;

        public fMessageCacheAttributes Attributes => mAttributes;

        public cEnvelope Envelope
        {
            get => mEnvelope;

            set
            {
                if (value == null) throw new ArgumentNullException();

                if (mEnvelope == null)
                {
                    mEnvelope = value;
                    mAttributes |= fMessageCacheAttributes.envelope;
                }
            }
        }

        public cTimestamp Received
        {
            get => mReceived;

            set
            {
                if (value == null) throw new ArgumentNullException();

                if (mReceived == null)
                {
                    mReceived = value;
                    mAttributes |= fMessageCacheAttributes.received;
                }
            }
        }

        public uint? Size => mSize;

        public void SetSize(uint pSize)
        {
            if (mSize == null)
            {
                mSize = pSize;
                mAttributes |= fMessageCacheAttributes.size;
            }
        }

        public cBodyPart BodyStructure
        {
            get => mBodyStructure;

            set
            {
                if (value == null) throw new ArgumentNullException();

                if (mBodyStructure == null)
                {
                    mBodyStructure = value;
                    mAttributes |= fMessageCacheAttributes.bodystructure;
                }
            }
        }

        public cHeaderFields HeaderFields => mHeaderFields;

        public void AddHeaderFields(cHeaderFields pHeaderFields)
        {
            mHeaderFields += pHeaderFields;
        }

        public cBinarySizes BinarySizes => mBinarySizes;

        public void AddBinarySizes(cBinarySizes pBinarySizes)
        {
            mBinarySizes += pBinarySizes;
        }
    }
}