using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Fetch(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandle);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            var lTask = ZFetchAttributesAsync(pMailboxId, lHandles, lToFetch, null, lContext);
            mEventSynchroniser.Wait(lTask, lContext);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
        }

        public void Fetch(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandles);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            var lTask = ZFetchAttributesAsync(pMailboxId, lHandles, lToFetch, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
        }

        public async Task FetchAsync(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandle);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            await ZFetchAttributesAsync(pMailboxId, lHandles, lToFetch, null, lContext).ConfigureAwait(false);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
        }

        public async Task FetchAsync(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lHandles = ZFetchHandles(pHandles);
            var lToFetch = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            await ZFetchAttributesAsync(pMailboxId, lHandles, lToFetch, pFC, lContext).ConfigureAwait(false);

            var lMissing = ZFetchAttributesToFetch(lHandles, lRequired);
            if (lMissing != 0) throw new cFetchFailedException();
        }

        private cHandleList ZFetchHandles(iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cHandleList(pHandle);
        }

        private cHandleList ZFetchHandles(IList<iMessageHandle> pHandles)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));

            object lMessageCache = null;

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains nulls");
                if (lMessageCache == null) lMessageCache = lHandle.MessageCache;
                else if (!ReferenceEquals(lHandle.MessageCache, lMessageCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed message caches");
            }

            return new cHandleList(pHandles);
        }
    }
}
