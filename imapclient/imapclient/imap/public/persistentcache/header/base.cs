using System;
using work.bacome.imapclient.support;
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

        public abstract cHeaderCacheItemData Update(iMessageHandle pMessageHandle);
    }
}
