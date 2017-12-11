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
        internal bool Fetch(iMessageHandle pMessageHandle, cMessageCacheItems pItems)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(Fetch), 1);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pMessageHandle.Contains(pItems)) return true;

            var lTask = ZFetchCacheItemsAsync(cMessageHandleList.FromMessageHandle(pMessageHandle), pItems, null, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return pMessageHandle.Contains(pItems);
        }

        internal cMessageHandleList Fetch(IEnumerable<iMessageHandle> pMessageHandles, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(Fetch), 2);

            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lMessageHandles = cMessageHandleList.FromMessageHandles(pMessageHandles);

            if (lMessageHandles.All(h => h.Contains(pItems))) return new cMessageHandleList();

            var lTask = ZFetchCacheItemsAsync(lMessageHandles, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return new cMessageHandleList(from h in lMessageHandles where !h.Contains(pItems) select h);
        }

        /// <summary>
        /// Ensures that the specified items are cached for the specified messages.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <param name="pItems"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns>A list of messages where something went wrong and the cache was not populated.</returns>
        /// <remarks>
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fMessageProperties"/>. This means that you can use values of those types as arguments to this method.</note>
        /// </remarks>
        public List<cMessage> Fetch(IEnumerable<cMessage> pMessages, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(Fetch), 3);

            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lMessageHandles = cMessageHandleList.FromMessages(pMessages);

            if (lMessageHandles.All(h => h.Contains(pItems))) return new List<cMessage>();

            var lTask = ZFetchCacheItemsAsync(lMessageHandles, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return new List<cMessage>(from m in pMessages where !m.MessageHandle.Contains(pItems) select m);
        }

        internal async Task<bool> FetchAsync(iMessageHandle pMessageHandle, cMessageCacheItems pItems)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(FetchAsync), 1);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pMessageHandle.Contains(pItems)) return true;

            await ZFetchCacheItemsAsync(cMessageHandleList.FromMessageHandle(pMessageHandle), pItems, null, lContext).ConfigureAwait(false);

            return pMessageHandle.Contains(pItems);
        }

        internal async Task<cMessageHandleList> FetchAsync(IEnumerable<iMessageHandle> pMessageHandles, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(FetchAsync), 2);

            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lMessageHandles = cMessageHandleList.FromMessageHandles(pMessageHandles);

            if (lMessageHandles.All(h => h.Contains(pItems))) return new cMessageHandleList();

            await ZFetchCacheItemsAsync(lMessageHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return new cMessageHandleList(from h in lMessageHandles where !h.Contains(pItems) select h);
        }

        /// <summary>
        /// Asynchronously ensures that the specified items are cached for the specified messages.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <param name="pItems"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Fetch(IEnumerable{cMessage}, cMessageCacheItems, cCacheItemFetchConfiguration)" select="returns|remarks"/>
        public async Task<List<cMessage>> FetchAsync(IEnumerable<cMessage> pMessages, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(true, nameof(cIMAPClient), nameof(FetchAsync), 3);

            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lMessageHandles = cMessageHandleList.FromMessages(pMessages);

            if (lMessageHandles.All(h => h.Contains(pItems))) return new List<cMessage>();

            await ZFetchCacheItemsAsync(lMessageHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return new List<cMessage>(from m in pMessages where !m.MessageHandle.Contains(pItems) select m);
        }

        private async Task ZFetchCacheItemsAsync(cMessageHandleList pMessageHandles, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchCacheItemsAsync), pMessageHandles, pItems);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pMessageHandles.Count == 0) return;
            if (pItems.IsEmpty) return;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    await lSession.FetchCacheItemsAsync(lMC, pMessageHandles, pItems, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                await lSession.FetchCacheItemsAsync(lMC, pMessageHandles, pItems, pConfiguration.Increment, lContext).ConfigureAwait(false);
            }
        }
    
        private async Task<List<cMessage>> ZUIDFetchCacheItemsAsync(iMailboxHandle pMailboxHandle, cUIDList pUIDs, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchCacheItemsAsync), pMailboxHandle, pUIDs, pItems);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pUIDs.Count == 0) return new List<cMessage>();
            if (pItems.IsEmpty) throw new ArgumentOutOfRangeException(nameof(pItems));

            cMessageHandleList lMessageHandles;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    lMessageHandles = await lSession.UIDFetchCacheItemsAsync(lMC, pMailboxHandle, pUIDs, pItems, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                lMessageHandles = await lSession.UIDFetchCacheItemsAsync(lMC, pMailboxHandle, pUIDs, pItems, pConfiguration.Increment, lContext).ConfigureAwait(false);
            }

            List<cMessage> lMessages = new List<cMessage>(lMessageHandles.Count);
            foreach (var lMessageHandle in lMessageHandles) lMessages.Add(new cMessage(this, lMessageHandle));
            return lMessages;
        }
    }
}
