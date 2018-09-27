using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFlagCache : cPersistentCacheComponent
    {
        ;?;
        protected cFlagCache(string pInstanceName, long pLastAccessSequenceNumber) : base(pInstanceName, pLastAccessSequenceNumber) { }
        public abstract bool TryGetItem(cMessageUID pMessageUID, out cFlagCacheItem rItem);
        protected internal abstract cFlagCacheItem GetItem(cMessageUID pMessageUID, cTrace.cContext pParentContext);
    }
}
