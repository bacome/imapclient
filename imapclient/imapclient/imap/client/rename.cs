using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task<cMailbox> RenameAsync(iMailboxHandle pMailboxHandle, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(RenameAsync), pMailboxHandle, pMailboxName);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            uint lUIDValidity = pMailboxHandle.MailboxStatus?.UIDValidity ?? 0;
            iMailboxHandle lNewMailboxHandle;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                lNewMailboxHandle = await lSession.RenameAsync(lMC, pMailboxHandle, pMailboxName, lContext).ConfigureAwait(false);
            }

            ZCacheIntegrationRename(pMailboxHandle.MailboxId, lUIDValidity, pMailboxName, lContext);

            return new cMailbox(this, lNewMailboxHandle);
        }
    }
}