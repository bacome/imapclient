using System;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cHeaderCacheItem
    {
        public cBodyPart BodyStructure { get; }
        public cEnvelope Envelope { get; }
        public DateTimeOffset? ReceivedDateTimeOffset { get; }
        public DateTime? ReceivedDateTime { get; }
        public uint? Size { get; }
        public cHeaderFields HeaderFields { get; } // note that this is dynamic
        public cBinarySizes BinarySizes { get; } // note that this is dynamic
    }
}
