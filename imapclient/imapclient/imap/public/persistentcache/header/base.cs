using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cHeaderCache : cPersistentCacheComponent
    {
        public readonly string InstanceName;
        protected readonly cTrace.cContext mRootContext;

        protected cHeaderCache(string pInstanceName)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
        }

        public bool TryGetHeaderCacheItem(cMessageUID pMessageUID, out cHeaderCacheItem rHeaderCacheItem) => TryGetHeaderCacheItem(pMessageUID, out rHeaderCacheItem, mRootContext);

        protected internal abstract bool TryGetHeaderCacheItem(cMessageUID pMessageUID, out cHeaderCacheItem rHeaderCacheItem, cTrace.cContext pParentContext);
    }
}
