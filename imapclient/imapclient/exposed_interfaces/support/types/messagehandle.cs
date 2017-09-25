using System;

namespace work.bacome.imapclient.support
{
    public interface iMessageHandle
    {
        iMessageCache Cache { get; }
        int CacheSequence { get; }
        bool Expunged { get; }
        fCacheAttributes Attributes { get; }
        cBodyPart Body { get; }
        cBodyPart BodyStructure { get; }
        cEnvelope Envelope { get; }
        cMessageFlags Flags { get; }
        ulong? ModSeq { get; }
        DateTime? Received { get; }
        uint? Size { get; }
        cUID UID { get; }
        cHeaderFields HeaderFields { get; }
        cBinarySizes BinarySizes { get; }

        bool Contains(cCacheItems pItems);
        bool ContainsNone(cCacheItems pItems);
        cCacheItems Missing(cCacheItems pItems);
    }
}
