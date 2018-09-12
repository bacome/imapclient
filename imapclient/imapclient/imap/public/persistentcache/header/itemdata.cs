using System;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    // TODO: this should be serializable (which means each element should be)
    public class cHeaderCacheItemData
    {
        public readonly cBodyPart BodyStructure;
        public readonly cEnvelope Envelope;
        public readonly DateTimeOffset? ReceivedDateTimeOffset;
        public readonly DateTime? ReceivedDateTime;
        public readonly uint? Size;
        public readonly cHeaderFields HeaderFields; // note that this is dynamic
        public readonly cBinarySizes BinarySizes; // note that this is dynamic
    }
}
