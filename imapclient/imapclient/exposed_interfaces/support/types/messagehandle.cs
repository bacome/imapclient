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
        DateTime? Received { get; }
        uint? Size { get; }
        cUID UID { get; }
        cHeaderValues HeaderValues { get; }
        cBinarySizes BinarySizes { get; }
        ulong? ModSeq { get; }
    }
}
