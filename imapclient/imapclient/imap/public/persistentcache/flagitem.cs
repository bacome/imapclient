using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public interface iFlagCacheItem
    {
        cModSeqFlags ModSeqFlags { get; }
        void Update(cModSeqFlags pModSeqFlags, cTrace.cContext pParentContext); // should not update if the modseq isn't greater (unless the modseq is zero, in which case it should)
    }
}
