using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Fetch(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchAsync(pMailboxId, ZFetchHandles(pHandle), pProperties, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public void Fetch(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchAsync(pMailboxId, ZFetchHandles(pHandles), pProperties, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task FetchAsync(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchAsync(pMailboxId, ZFetchHandles(pHandle), pProperties, lContext);
        }

        public Task FetchAsync(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchAsync(pMailboxId, ZFetchHandles(pHandles), pProperties, lContext);
        }

        private cHandleList ZFetchHandles(iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            return new cHandleList(pHandle);
        }

        private cHandleList ZFetchHandles(IList<iMessageHandle> pHandles)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));

            iMessageCache lCache = null;

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains nulls");
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed caches");
            }

            return new cHandleList(pHandles);
        }

        private async Task ZFetchAsync(cMailboxId pMailboxId, cHandleList pHandles, fMessageProperties pProperties, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchAsync), pMailboxId, pHandles, pProperties);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            if (pHandles.Count == 0) return;

            // must have specified some properties to get, there is no default for fetch
            if ((pProperties & fMessageProperties.allmask) == 0 || (pProperties & fMessageProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException(nameof(pProperties));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);
                await lSession.FetchAsync(lMC, pMailboxId, pHandles, pProperties, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}
