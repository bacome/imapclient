using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<iMessageHandle> GetMessageHandleAsync(iMailboxHandle pMailboxHandle, cUID pUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetMessageHandleAsync), pMailboxHandle, pUID);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));

            cMessageHandleList lMessageHandles;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                lMessageHandles = await lSession.UIDFetchCacheItemsAsync(lMC, pMailboxHandle, cUIDList.FromUID(pUID), cMessageCacheItems.UID, null, lContext).ConfigureAwait(false);
            }

            if (lMessageHandles.Count == 0) return null;
            if (lMessageHandles.Count == 1) return lMessageHandles[0];
            throw new cInternalErrorException(lContext);
        }
    }
}