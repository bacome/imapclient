using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iHeaderDataItem
    {
        cEnvelope Envelope { get; }
        cTimestamp Received { get; }
        uint? Size { get; }
        cBodyPart BodyStructure { get; }
        cHeaderFields HeaderFields { get; }
        cBinarySizes BinarySizes { get; }
    }

    public interface iHeaderCacheItem : iHeaderDataItem
    {
        void Update(iHeaderDataItem pHeaderDataItem, cTrace.cContext pParentContext); // updates this instance from the supplied instance
    }
}
