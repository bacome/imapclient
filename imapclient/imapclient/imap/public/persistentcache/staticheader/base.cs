using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cStaticHeaderCache : cPersistentCacheComponent
    {
        protected cStaticHeaderCache(string pInstanceName, long pLastAccessSequenceNumber) : base(pInstanceName, pLastAccessSequenceNumber) { }
        public abstract bool TryGetItem(cMessageUID pMessageUID, out cStaticHeaderCacheItem rItem);
        protected internal abstract cStaticHeaderCacheItem GetItem(cMessageUID pMessageUID, cTrace.cContext pParentContext);
    }
}
