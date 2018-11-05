using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public interface iFlagDataItem
    {
        cModSeqFlags ModSeqFlags { get; }
    }

    public interface iFlagCacheItem : iFlagDataItem
    {
        fMessageCacheAttributes Attributes { get; }
        void Update(iFlagDataItem pFlagDataItem, cTrace.cContext pParentContext);
    }
}
