using System;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public interface iSectionCacheItem
    {
        bool TryGetReadStream(out Stream rStream, cTrace.cContext pParentContext);
        bool CanGetReadStream(cTrace.cContext pParentContext);
        bool SetAdded(cTrace.cContext pParentContext);
    }
}
