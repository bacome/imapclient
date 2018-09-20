using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cHeaderCache : cPersistentCacheComponent
    {
        protected cHeaderCache(string pInstanceName, long pLastAccessSequenceNumber) : base(pInstanceName, pLastAccessSequenceNumber) { }
        public abstract bool TryGetItem(cMessageUID pMessageUID, out cHeaderCacheItem rItem);
        protected internal abstract cHeaderCacheItem GetItem(cMessageUID pMessageUID, cTrace.cContext pParentContext);
    }
}
