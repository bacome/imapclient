using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public bool Fetch(iMessageHandle pHandle, cCacheItems pItems)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pHandle.ContainsAll(pItems)) return true;

            var lTask = ZFetchCacheItemsAsync(ZFetchHandles(pHandle), pItems, null, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return pHandle.ContainsAll(pItems);
        }

        public bool Fetch(IList<iMessageHandle> pHandles, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lHandles = ZFetchHandles(pHandles);

            if (lHandles.AllContainAll(pItems)) return true;

            var lTask = ZFetchCacheItemsAsync(lHandles, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return lHandles.AllContainAll(pItems);
        }

        public async Task<bool> FetchAsync(iMessageHandle pHandle, cCacheItems pItems)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pHandle.ContainsAll(pItems)) return true;

            await ZFetchCacheItemsAsync(ZFetchHandles(pHandle), pItems, null, lContext).ConfigureAwait(false);

            return pHandle.ContainsAll(pItems);
        }

        public async Task<bool> FetchAsync(IList<iMessageHandle> pHandles, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(FetchAsync));

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lHandles = ZFetchHandles(pHandles);

            if (lHandles.AllContainAll(pItems)) return true;

            await ZFetchCacheItemsAsync(lHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return lHandles.AllContainAll(pItems);
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

        private async Task ZFetchCacheItemsAsync(cMessageHandleList pHandles, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchCacheItemsAsync), pHandles, pItems);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pHandles.Count == 0) return;
            if (pItems.IsNone) return;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lProgress = new cProgress();
                    await lSession.FetchCacheItemsAsync(lMC, pHandles, pItems, lProgress, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lProgress = new cProgress(mSynchroniser, pConfiguration.Increment);
                await lSession.FetchCacheItemsAsync(lMC, pHandles, pItems, lProgress, lContext).ConfigureAwait(false);
            }
        }

        private async Task<List<cMessage>> ZUIDFetchCacheItemsAsync(iMailboxHandle pHandle, cUIDList pUIDs, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchCacheItemsAsync), pHandle, pUIDs, pItems);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pUIDs.Count == 0) return new List<cMessage>();
            if (pItems.IsNone) throw new ArgumentOutOfRangeException(nameof(pItems));

            cMessageHandleList lHandles;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lProgress = new cProgress();
                    lHandles = await lSession.UIDFetchAttributesAsync(lMC, pHandle, pUIDs, pItems, lProgress, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lProgress = new cProgress(mSynchroniser, pConfiguration.Increment);
                lHandles = await lSession.UIDFetchAttributesAsync(lMC, pHandle, pUIDs, pItems, lProgress, lContext).ConfigureAwait(false);
            }

            List<cMessage> lMessages = new List<cMessage>(lHandles.Count);
            foreach (var lHandle in lHandles) lMessages.Add(new cMessage(this, lHandle));
            return lMessages;
        }


    }
}
