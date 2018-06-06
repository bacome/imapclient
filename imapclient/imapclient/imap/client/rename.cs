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

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                var lMailboxHandle = await lSession.RenameAsync(lMC, pMailboxHandle, pMailboxName, lContext).ConfigureAwait(false);
                return new cMailbox(this, lMailboxHandle);
            }

            if (pMailboxHandle.MailboxName.IsInbox)
            {

            }
            else
            { 
                if (lUIDValidity != 0) ZRename(pMailboxHandle.MailboxId, lUIDValidity, pMailboxName, lContext);

                ;?; // and children ...
                ZAddMailboxUIDValidity(pMailboxHandle.MailboxId, 0, lContext);
            }
        }
    }
}