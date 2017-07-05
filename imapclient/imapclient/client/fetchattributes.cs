using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Fetch(cMailboxId pMailboxId, iMessageHandle pHandle, fFetchAttributes pAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchAsync(pMailboxId, ZFetchHandles(pHandle), pAttributes, null, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public void Fetch(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fFetchAttributes pAttributes, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));
            var lTask = ZFetchAsync(pMailboxId, ZFetchHandles(pHandles), pAttributes, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task FetchAsync(cMailboxId pMailboxId, iMessageHandle pHandle, fFetchAttributes pAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchAsync(pMailboxId, ZFetchHandles(pHandle), pAttributes, null, lContext);
        }

        public Task FetchAsync(cMailboxId pMailboxId, IList<iMessageHandle> pHandles, fFetchAttributes pAttributes, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));
            return ZFetchAsync(pMailboxId, ZFetchHandles(pHandles), pAttributes, pFC, lContext);
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

        private async Task ZFetchAsync(cMailboxId pMailboxId, cHandleList pHandles, fFetchAttributes pAttributes, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchAsync), pMailboxId, pHandles, pAttributes, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            if (pHandles.Count == 0) return;

            // must have specified some attributes to get, there is no default for fetch
            if ((pAttributes & fFetchAttributes.allmask) == 0 || (pAttributes & fFetchAttributes.clientdefault) != 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchAttributesMethodControl lMC;
                if (pFC == null) lMC = new cFetchAttributesMethodControl(mTimeout, CancellationToken, null);
                else lMC = new cFetchAttributesMethodControl(pFC.Timeout, pFC.CancellationToken, pFC.IncrementProgress);
                await lSession.FetchAsync(lMC, pMailboxId, pHandles, pAttributes, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

        private class cFetchAttributesMethodControl : cMethodControl
        {
            private readonly Action<int> mIncrementProgress;

            public cFetchAttributesMethodControl(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrementProgress) : base(pTimeout, pCancellationToken)
            {
                mIncrementProgress = pIncrementProgress;
            }

            public void IncrementProgress(int pValue) => mIncrementProgress?.Invoke(pValue);
        }
    }
}
