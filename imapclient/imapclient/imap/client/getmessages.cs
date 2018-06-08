using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<IEnumerable<cIMAPMessage>> GetMessagesAsync(iMailboxHandle pMailboxHandle, cUIDList pUIDs, cMessageCacheItems pItems, cIncrementConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetMessagesAsync), pMailboxHandle, pUIDs, pItems, pConfiguration);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pUIDs.Count == 0) return new List<cIMAPMessage>();
            if (pItems.IsEmpty) throw new ArgumentOutOfRangeException(nameof(pItems));

            cMessageHandleList lMessageHandles;

            if (pConfiguration == null)
            {
                using (var lToken = CancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    lMessageHandles = await lSession.UIDFetchCacheItemsAsync(lMC, pMailboxHandle, pUIDs, pItems, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                lMessageHandles = await lSession.UIDFetchCacheItemsAsync(lMC, pMailboxHandle, pUIDs, pItems, pConfiguration.Increment, lContext).ConfigureAwait(false);
            }

            return lMessageHandles.Select(lMessageHandle => new cIMAPMessage(this, lMessageHandle));
        }

        internal async Task<IEnumerable<cIMAPMessage>> GetMessagesAsync(iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cSetMaximumConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetMessagesAsync), pMailboxHandle, pFilter, pSort, pItems, pConfiguration);

            IEnumerable<iMessageHandle> lMessageHandles;

            if (pConfiguration == null)
            {
                using (var lToken = CancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    lMessageHandles = await ZGetMessagesAsync(lMC, pMailboxHandle, pFilter, pSort, pItems, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                lMessageHandles = await ZGetMessagesAsync(lMC, pMailboxHandle, pFilter, pSort, pItems, pConfiguration, lContext).ConfigureAwait(false);
            }

            return lMessageHandles.Select(lMessageHandle => new cIMAPMessage(this, lMessageHandle));
        }

        private async Task<IEnumerable<iMessageHandle>> ZGetMessagesAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cSetMaximumConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetMessagesAsync), pMC, pMailboxHandle, pFilter, pSort, pItems, pConfiguration);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
            if (pSort == null) throw new ArgumentNullException(nameof(pSort));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            cMessageHandleList lMessageHandles;

            if (ReferenceEquals(pSort, cSort.None))
            {
                // no sorting

                if (lSession.Capabilities.ESearch) lMessageHandles = await lSession.SearchExtendedAsync(pMC, pMailboxHandle, pFilter, lContext).ConfigureAwait(false);
                else lMessageHandles = await lSession.SearchAsync(pMC, pMailboxHandle, pFilter, lContext).ConfigureAwait(false);

                await ZGetMessagesFetchAsync(pMC, lSession, lMessageHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);
            }
            else
            {
                var lSortAttributes = pSort.Attributes(out var lSortDisplay);

                if (!lSortDisplay && lSession.Capabilities.Sort || lSortDisplay && lSession.Capabilities.SortDisplay)
                {
                    // server side sorting

                    if (lSession.Capabilities.ESort) lMessageHandles = await lSession.SortExtendedAsync(pMC, pMailboxHandle, pFilter, pSort, lContext).ConfigureAwait(false);
                    else lMessageHandles = await lSession.SortAsync(pMC, pMailboxHandle, pFilter, pSort, lContext).ConfigureAwait(false);

                    await ZGetMessagesFetchAsync(pMC, lSession, lMessageHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);
                }
                else
                {
                    // client side sorting

                    if (lSession.Capabilities.ESearch) lMessageHandles = await lSession.SearchExtendedAsync(pMC, pMailboxHandle, pFilter, lContext).ConfigureAwait(false);
                    else lMessageHandles = await lSession.SearchAsync(pMC, pMailboxHandle, pFilter, lContext).ConfigureAwait(false);

                    var lItems = new cMessageCacheItems(pItems.Attributes | lSortAttributes, pItems.Names);
                    await ZGetMessagesFetchAsync(pMC, lSession, lMessageHandles, lItems, pConfiguration, lContext).ConfigureAwait(false);

                    lMessageHandles.Sort(pSort);
                }
            }

            return lMessageHandles;
        }

        private Task ZGetMessagesFetchAsync(cMethodControl pMC, cSession pSession, cMessageHandleList pMessageHandles, cMessageCacheItems pItems, cSetMaximumConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetMessagesFetchAsync), pMC, pMessageHandles, pItems, pConfiguration);

            if (pMessageHandles.Count == 0) return Task.WhenAll();
            if (pItems.IsEmpty) return Task.WhenAll();

            if (pMessageHandles.TrueForAll(h => h.Contains(pItems))) return Task.WhenAll();

            Action<int> lIncrement;

            if (pConfiguration == null) lIncrement = null;
            else
            {
                mSynchroniser.InvokeActionLong(pConfiguration.SetMaximum, pMessageHandles.Count, lContext);
                lIncrement = pConfiguration.Increment;
            }

            return pSession.FetchCacheItemsAsync(pMC, pMessageHandles, pItems, lIncrement, lContext);
        }
    }
}