using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public bool Fetch(iMessageHandle pHandle, cMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pProperties == null) throw new ArgumentNullException(nameof(pProperties));

            var lAttributes = new cFetchAttributes(pProperties);
            if (pHandle.ContainsAll(lAttributes)) return true;

            var lTask = ZFetchAttributesAsync(ZFetchHandles(pHandle), lAttributes, null, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return pHandle.ContainsAll(lAttributes);
        }

        public bool Fetch(IList<iMessageHandle> pHandles, cMessageProperties pProperties, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pProperties == null) throw new ArgumentNullException(nameof(pProperties));

            var lHandles = ZFetchHandles(pHandles);

            var lAttributes = new cFetchAttributes(pProperties);
            if (lHandles.AllContainAll(lAttributes)) return true;

            var lTask = ZFetchAttributesAsync(lHandles, lAttributes, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return lHandles.AllContainAll(lAttributes);
        }

        public async Task<bool> FetchAsync(iMessageHandle pHandle, cMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pProperties == null) throw new ArgumentNullException(nameof(pProperties));

            var lAttributes = new cFetchAttributes(pProperties);
            if (pHandle.ContainsAll(lAttributes)) return true;

            await ZFetchAttributesAsync(ZFetchHandles(pHandle), lAttributes, null, lContext).ConfigureAwait(false);

            return pHandle.ContainsAll(lAttributes);
        }

        public async Task<bool> FetchAsync(IList<iMessageHandle> pHandles, cMessageProperties pProperties, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pProperties == null) throw new ArgumentNullException(nameof(pProperties));

            var lHandles = ZFetchHandles(pHandles);

            var lAttributes = new cFetchAttributes(pProperties);
            if (lHandles.AllContainAll(lAttributes)) return true;

            await ZFetchAttributesAsync(lHandles, lAttributes, pConfiguration, lContext).ConfigureAwait(false);

            return lHandles.AllContainAll(lAttributes);
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
