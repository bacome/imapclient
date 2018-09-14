using System;
using work.bacome.imapclient.support;
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

        public abstract void Touch(iMessageHandle pMessageHandle); // called when there is a 'get' of the ModSeq/Flag data and the UID is known
        public abstract cFlagCacheItem Update(iMessageHandle pMessageHandle); // called when there is a 'set' of the ModSeq/Flag data and the UID is known, or when there is a 'set' of the UID, should return what the cache currently understands are the values
    }
}
