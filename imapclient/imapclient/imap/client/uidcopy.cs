using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<cCopyFeedback> UIDCopyAsync(iMailboxHandle pSourceMailboxHandle, cUIDList pSourceUIDs, iMailboxHandle pDestinationMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(UIDCopyAsync), pSourceMailboxHandle, pSourceUIDs, pDestinationMailboxHandle);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pSourceMailboxHandle == null) throw new ArgumentNullException(nameof(pSourceMailboxHandle));
            if (pSourceUIDs == null) throw new ArgumentNullException(nameof(pSourceUIDs));
            if (pDestinationMailboxHandle == null) throw new ArgumentNullException(nameof(pDestinationMailboxHandle));

            if (pSourceUIDs.Count == 0) return null;

            cCopyFeedback lFeedback;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                lFeedback = await lSession.UIDCopyAsync(lMC, pSourceMailboxHandle, pSourceUIDs, pDestinationMailboxHandle, lContext).ConfigureAwait(false);
            }

            if (lFeedback != null && lFeedback.Count > 0) ZCacheIntegrationCopy(pSourceMailboxHandle.MailboxId, pDestinationMailboxHandle.MailboxName, lFeedback, lContext);

            return lFeedback;
        }
    }
}