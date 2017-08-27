using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public bool Fetch(iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return true; // nothing to do

            var lHandles = ZFetchHandles(pHandle);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return true; // got everything already

            var lTask = ZFetchAttributesAsync(lHandles, lToFetch, null, lContext);
            mEventSynchroniser.Wait(lTask, lContext);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            return lMissing == 0;
        }

        public bool Fetch(IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchConfiguration pConfig)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return true; // nothing to do

            var lHandles = ZFetchHandles(pHandles);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return true; // got everything already

            var lTask = ZFetchAttributesAsync(lHandles, lToFetch, pConfig, lContext);
            mEventSynchroniser.Wait(lTask, lContext);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            return lMissing == 0;
        }

        public async Task<bool> FetchAsync(iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return true; // nothing to do

            var lHandles = ZFetchHandles(pHandle);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return true; // got everything already

            await ZFetchAttributesAsync(lHandles, lToFetch, null, lContext).ConfigureAwait(false);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            return lMissing == 0;
        }

        public async Task<bool> FetchAsync(IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchConfiguration pConfig)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return true; // nothing to do

            var lHandles = ZFetchHandles(pHandles);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return true; // got everything already

            await ZFetchAttributesAsync(lHandles, lToFetch, pConfig, lContext).ConfigureAwait(false);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            return lMissing == 0;
        }

        private cMessageHandleList ZFetchHandles(iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cMessageHandleList(pHandle);
        }

        private cMessageHandleList ZFetchHandles(IList<iMessageHandle> pHandles)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));

            object lCache = null;

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains nulls");
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed caches");
            }

            return new cMessageHandleList(pHandles);
        }
    }
}
