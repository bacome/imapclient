using System;

namespace work.bacome.imapclient
{
    public interface iMessageHandle
    {
        iMessageCache Cache { get; }
        int CacheSequence { get; }
        bool Expunged { get; }
        fMessageProperties Properties { get; }
        cBodyPart Body { get; }
        cBodyPart BodyStructure { get; }
        cEnvelope Envelope { get; }
        cFetchedFlags Flags { get; }
        DateTime? Received { get; }
        uint? Size { get; }
        cUID UID { get; }
        cStrings References { get; }
        cBinarySizes BinarySizes { get; } // part => size
    }
}
