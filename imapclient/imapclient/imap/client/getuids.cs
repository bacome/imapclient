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
        internal async Task<IEnumerable<cUID>> GetUIDsAsync(iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cSetMaximumConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetUIDsAsync), pMailboxHandle, pFilter, pSort, pConfiguration);

            if (pConfiguration == null)
            {
                using (var lToken = CancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    return await ZGetUIDsAsync(lMC, pMailboxHandle, pFilter, pSort, null, lContext).ConfigureAwait(false);
                }
            }
            else return await ZGetUIDsAsync(pConfiguration.MC, pMailboxHandle, pFilter, pSort, pConfiguration, lContext).ConfigureAwait(false);
        }

        private Task<IEnumerable<cUID>> ZGetUIDsAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cSetMaximumConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetUIDsAsync), pMC, pMailboxHandle, pFilter, pSort, pConfiguration);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
            if (pSort == null) throw new ArgumentNullException(nameof(pSort));

            if (ReferenceEquals(pSort, cSort.None))
            {
                // no sorting
                if (lSession.Capabilities.ESearch) return lSession.UIDSearchExtendedAsync(pMC, pMailboxHandle, pFilter, lContext);
                else return lSession.UIDSearchAsync(pMC, pMailboxHandle, pFilter, lContext);
            }
            else
            {
                var lSortAttributes = pSort.Attributes(out var lSortDisplay);

                if (!lSortDisplay && lSession.Capabilities.Sort || lSortDisplay && lSession.Capabilities.SortDisplay)
                {
                    // server side sorting
                    if (lSession.Capabilities.ESort) return lSession.UIDSortExtendedAsync(pMC, pMailboxHandle, pFilter, pSort, lContext);
                    else return lSession.UIDSortAsync(pMC, pMailboxHandle, pFilter, pSort, lContext);
                }
                else
                {
                    // client side sorting
                    return ZZGetUIDsAsync(pMC, pMailboxHandle, pFilter, pSort, pConfiguration, lContext);
                }
            }
        }

        private async Task<IEnumerable<cUID>> ZZGetUIDsAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cSetMaximumConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZGetUIDsAsync), pMC, pMailboxHandle, pFilter, pSort, pConfiguration);
            var lMessageHandles = await ZGetMessagesAsync(pMC, pMailboxHandle, pFilter, pSort, cMessageCacheItems.UID, pConfiguration, lContext).ConfigureAwait(false);
            return lMessageHandles.Select(lMessageHandle => lMessageHandle.UID);
        }
    }
}