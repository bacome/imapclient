using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal bool Fetch(iMessageHandle pHandle, cCacheItems pItems)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(Fetch), 1);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pHandle.Contains(pItems)) return true;

            var lTask = ZFetchCacheItemsAsync(cMessageHandleList.FromHandle(pHandle), pItems, null, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return pHandle.Contains(pItems);
        }

        internal cMessageHandleList Fetch(IEnumerable<iMessageHandle> pHandles, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(Fetch), 2);

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lHandles = cMessageHandleList.FromHandles(pHandles);

            if (lHandles.All(h => h.Contains(pItems))) return new cMessageHandleList();

            var lTask = ZFetchCacheItemsAsync(lHandles, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return new cMessageHandleList(from h in lHandles where !h.Contains(pItems) select h);
        }

        // TODO: work out why the following XML generates such ugly documentation
        //  (there are plenty more examples of ugly so this may indicate a larger problem)

        /// <summary>
        /// <para>For a set of messages ensure that the specified items are cached.</para>
        /// </summary>
        /// <param name="pMessages">The set of messages.</param>
        /// <param name="pItems">The set of cache items.</param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns>A list of messages where the cache does NOT contain the requested items (i.e. where the fetch failed).</returns>
        public List<cMessage> Fetch(IEnumerable<cMessage> pMessages, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(Fetch), 3);

            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lHandles = cMessageHandleList.FromMessages(pMessages);

            if (lHandles.All(h => h.Contains(pItems))) return new List<cMessage>();

            var lTask = ZFetchCacheItemsAsync(lHandles, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return new List<cMessage>(from m in pMessages where !m.Handle.Contains(pItems) select m);
        }

        internal async Task<bool> FetchAsync(iMessageHandle pHandle, cCacheItems pItems)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(FetchAsync), 1);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pHandle.Contains(pItems)) return true;

            await ZFetchCacheItemsAsync(cMessageHandleList.FromHandle(pHandle), pItems, null, lContext).ConfigureAwait(false);

            return pHandle.Contains(pItems);
        }

        internal async Task<cMessageHandleList> FetchAsync(IEnumerable<iMessageHandle> pHandles, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(FetchAsync), 2);

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lHandles = cMessageHandleList.FromHandles(pHandles);

            if (lHandles.All(h => h.Contains(pItems))) return new cMessageHandleList();

            await ZFetchCacheItemsAsync(lHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return new cMessageHandleList(from h in lHandles where !h.Contains(pItems) select h);
        }

        /**<summary>The async version of <see cref="Fetch(IEnumerable{cMessage}, cCacheItems, cPropertyFetchConfiguration)"/>.</summary>*/
        public async Task<List<cMessage>> FetchAsync(IEnumerable<cMessage> pMessages, cCacheItems pItems, cPropertyFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(FetchAsync), 3);

            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lHandles = cMessageHandleList.FromMessages(pMessages);

            if (lHandles.All(h => h.Contains(pItems))) return new List<cMessage>();

            await ZFetchCacheItemsAsync(lHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return new List<cMessage>(from m in pMessages where !m.Handle.Contains(pItems) select m);
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
                    lHandles = await lSession.UIDFetchCacheItemsAsync(lMC, pHandle, pUIDs, pItems, lProgress, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lProgress = new cProgress(mSynchroniser, pConfiguration.Increment);
                lHandles = await lSession.UIDFetchCacheItemsAsync(lMC, pHandle, pUIDs, pItems, lProgress, lContext).ConfigureAwait(false);
            }

            List<cMessage> lMessages = new List<cMessage>(lHandles.Count);
            foreach (var lHandle in lHandles) lMessages.Add(new cMessage(this, lHandle));
            return lMessages;
        }
    }
}
