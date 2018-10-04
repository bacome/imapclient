using System;
using work.bacome.mailclient;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iHeaderItem
    {
        fMessageCacheAttributes Attributes { get; }
        cEnvelope Envelope { get; set; }
        cTimestamp Received { get; set; }
        uint? Size { get; }
        void SetSize(uint pSize);
        cBodyPart BodyStructure { get; set; }
        cHeaderFields HeaderFields { get; }
        void AddHeaderFields(cHeaderFields pHeaderFields);
        cBinarySizes BinarySizes { get; }
        void AddBinarySizes(cBinarySizes pBinarySizes);
    }
}
