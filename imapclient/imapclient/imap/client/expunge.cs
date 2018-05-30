using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task ExpungeAsync(iMailboxHandle pMailboxHandle, bool pAndUnselect, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ExpungeAsync), pMailboxHandle, pAndUnselect);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            cMessageHandleList lDeletedMessages;

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);

                if (pAndUnselect) lDeletedMessages = await lSession.CloseAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                else
                {
                    await lSession.ExpungeAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                    lDeletedMessages = null;
                }
            }

            if (lDeletedMessages != null && lDeletedMessages.Count > 0)
            {
                var lSectionCache = SectionCache;
                lSectionCache.Deleted(lDeletedMessages, lContext);
            }
        }
    }
}