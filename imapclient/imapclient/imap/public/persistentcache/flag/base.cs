using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFlagCache : cPersistentCacheComponent
    {
        public readonly string InstanceName;
        protected readonly cTrace.cContext mRootContext;

        protected cFlagCache(string pInstanceName)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
        }

        public abstract iFlagCacheItem GetFlagCacheItem(cMessageUID pMessageUID);
    }
}
