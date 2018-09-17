using System;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iHeaderCacheItem
    {
        cEnvelope Envelope { get; set; } // nullable, but can't be set to null
        DateTimeOffset? ReceivedDateTimeOffset { get; } // nullable, but can't be set to null, and must be set in a pair with the following
        DateTime? ReceivedDateTime { get; } // nullable, but can't be set to null
        void SetReceivedDateTime(DateTimeOffset pOffset, DateTime pDateTime);
        uint? Size { get; set; } // nullable, but can't be set to null
        cBodyPart Body { get; set; } // nullable, but can't be set to null
        cBodyPart BodyStructure { get; set; } // nullable, but can't be set to null
        cHeaderFields HeaderFields { get; } // note that this is dynamic AND updates to it must be synchronised in the persistent cache
        cBinarySizes BinarySizes { get; } // note that this is dynamic AND updates to it must be synchronised in the persistent cache
        void AddHeaderFields(cHeaderFields pHeaderFields); // merge while locked
        void AddBinarySizes(cBinarySizes pBinarySizes); // merge while locked
    }
}
