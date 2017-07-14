using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iMessageHandle
    {
        object MessageCache { get; }
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
        cStrings References { get; }
        cBinarySizes BinarySizes { get; } // part => size
        ulong? ModSeq { get; }
    }
}
