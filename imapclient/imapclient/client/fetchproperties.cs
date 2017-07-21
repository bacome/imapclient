using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Fetch(iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandle);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            var lTask = ZFetchAttributesAsync(lHandles, lToFetch, null, lContext);
            mEventSynchroniser.Wait(lTask, lContext);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
        }

        public void Fetch(IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandles);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            var lTask = ZFetchAttributesAsync(lHandles, lToFetch, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
        }

        public async Task FetchAsync(iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandle);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            await ZFetchAttributesAsync(lHandles, lToFetch, null, lContext).ConfigureAwait(false);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
        }

        public async Task FetchAsync(IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandles);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            await ZFetchAttributesAsync(lHandles, lToFetch, pFC, lContext).ConfigureAwait(false);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
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
