using System;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cHeaderCacheItem
    {
        public abstract cEnvelope Envelope { get; protected internal set; } // nullable, but can't be set to null
        public abstract DateTimeOffset? ReceivedDateTimeOffset { get; } // nullable, but can't be set to null, and must be set in a pair with the following
        public abstract DateTime? ReceivedDateTime { get; } // nullable, but can't be set to null
        protected internal abstract void SetReceivedDateTime(DateTimeOffset pOffset, DateTime pDateTime);
        public abstract uint? Size { get; protected internal set; } // nullable, but can't be set to null
        public abstract cBodyPart Body { get; protected internal set; } // nullable, but can't be set to null
        public abstract cBodyPart BodyStructure { get; protected internal set; } // nullable, but can't be set to null
        public abstract cHeaderFields HeaderFields { get; } // note that this is dynamic AND updates to it must be synchronised in the persistent cache
        public abstract cBinarySizes BinarySizes { get; } // note that this is dynamic AND updates to it must be synchronised in the persistent cache
        protected internal abstract void AddHeaderFields(cHeaderFields pHeaderFields); // merge while locked
        protected internal abstract void AddBinarySizes(cBinarySizes pBinarySizes); // merge while locked
    }
}
