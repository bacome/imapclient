using System;
using System.Threading;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderCacheItem
    {
        private static long mAccessSequenceSource = 7; 

        private long mAccessSequence = 0;

        private object mAttributesLock = new object();
        private fMessageCacheAttributes mAttributes;

        private cEnvelope mEnvelope;
        private DateTimeOffset? mReceivedDateTimeOffset;
        private DateTime? mReceivedDateTime;
        private uint? mSize;
        private cBodyPart mBody;
        private cBodyPart mBodyStructure;

        private object mHeaderFieldsLock = new object();
        private cHeaderFields mHeaderFields;

        private object mBinarySizesLock = new object();
        private cBinarySizes mBinarySizes;

        protected internal cHeaderCacheItem()
        {
            mAttributes = 0;
            mEnvelope = null;
            mReceivedDateTimeOffset = null;
            mReceivedDateTime = null;
            mSize = null;
            mBody = null;
            mBodyStructure = null;
            mHeaderFields = null;
            mBinarySizes = null;
        }

        protected long AccessSequence => mAccessSequence;

        public cEnvelope Envelope
        {
            get
            {
                ;/; // use similar technique in flags EXCEPT that flag value doesn't change as 
                mAccessSequence = Interlocked.Increment(ref mAccessSequenceSource);
                return mEnvelope;
            }

            internal set
            {
                mEnvelope = value ?? throw new ArgumentNullException();

                lock (mAttributesLock)
                {
                    mAttributes |= fMessageCacheAttributes.envelope;
                }
            }
        }

        ;?; // remove virtual
        public virtual DateTimeOffset? ReceivedDateTimeOffset { get; protected set; } // nullable, but can't be set to null
        public virtual DateTime? ReceivedDateTime { get; protected set; } // nullable, but can't be set to null

        protected internal virtual void SetReceivedDateTime(DateTimeOffset pOffset, DateTime pDateTime)
        {
            ReceivedDateTimeOffset = pOffset;
            ReceivedDateTime = pDateTime;
        }

        public virtual uint? Size { get; protected internal set; } // nullable, but can't be set to null
        public virtual cBodyPart Body { get; protected internal set; } // nullable, but can't be set to null
        public virtual cBodyPart BodyStructure { get; protected internal set; } // nullable, but can't be set to null

        public virtual cHeaderFields HeaderFields { get; protected set; } // note that this is dynamic AND updates to it must be synchronised in the persistent cache

        protected internal virtual void AddHeaderFields(cHeaderFields pHeaderFields)
        {
            // must lock
            ;?;
        }


        public virtual cBinarySizes BinarySizes { get; protected set; } // note that this is dynamic AND updates to it must be synchronised in the persistent cache

        protected internal virtual void AddBinarySizes(cBinarySizes pBinarySizes); // merge while locked
    }
}
