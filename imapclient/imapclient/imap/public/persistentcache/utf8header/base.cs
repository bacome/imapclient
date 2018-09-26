using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cUTF8HeaderCache : cPersistentCacheComponent
    {
        protected cUTF8HeaderCache(string pInstanceName, long pLastAccessSequenceNumber) : base(pInstanceName, pLastAccessSequenceNumber) { }
        public abstract bool TryGetItem(cMessageUID pMessageUID, bool pUTF8, out cUTF8HeaderCacheItem rItem);
        protected internal abstract cUTF8HeaderCacheItem GetItem(cMessageUID pMessageUID, bool UTF8, cTrace.cContext pParentContext);
    }
}
