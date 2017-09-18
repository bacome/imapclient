using System;

namespace work.bacome.imapclient.support
{
    public interface iMessageHandle
    {
        iMessageCache Cache { get; }
        int CacheSequence { get; }
        bool Expunged { get; }
        fFetchAttributes Attributes { get; }
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

        bool ContainsAll(cFetchAttributes pAttributes);
        bool ContainsNone(cFetchAttributes pAttributes);
        cFetchAttributes Missing(cFetchAttributes pAttributes);
    }
}
