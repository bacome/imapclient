﻿using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<cCopyFeedback> CopyAsync(cMessageHandleList pSourceMessageHandles, iMailboxHandle pDestinationMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(CopyAsync), pSourceMessageHandles, pDestinationMailboxHandle);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pSourceMessageHandles == null) throw new ArgumentNullException(nameof(pSourceMessageHandles));
            if (pDestinationMailboxHandle == null) throw new ArgumentNullException(nameof(pDestinationMailboxHandle));

            if (pSourceMessageHandles.Count == 0) return null;

            cCopyFeedback lFeedback;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                lFeedback = await lSession.CopyAsync(lMC, pSourceMessageHandles, pDestinationMailboxHandle, lContext).ConfigureAwait(false);
            }

            if (lFeedback != null && lFeedback.Count > 0) ZCacheIntegrationCopy(pSourceMessageHandles[0].MessageCache.MailboxHandle.MailboxId, pDestinationMailboxHandle.MailboxName, lFeedback, lContext);

            return lFeedback;
        }
    }
}