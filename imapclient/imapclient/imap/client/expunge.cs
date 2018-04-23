using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Expunge(iMailboxHandle pMailboxHandle, bool pAndUnselect)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Expunge));
            var lTask = ZExpungeAsync(pMailboxHandle, pAndUnselect, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task ExpungeAsync(iMailboxHandle pMailboxHandle, bool pAndUnselect)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ExpungeAsync));
            return ZExpungeAsync(pMailboxHandle, pAndUnselect, lContext);
        }

        private async Task ZExpungeAsync(iMailboxHandle pMailboxHandle, bool pAndUnselect, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZExpungeAsync), pMailboxHandle, pAndUnselect);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                if (pAndUnselect) await lSession.CloseAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                else await lSession.ExpungeAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}