using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Expunge(iMailboxHandle pHandle, bool pAndUnselect)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Expunge));
            var lTask = ZExpungeAsync(pHandle, pAndUnselect, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        public Task ExpungeAsync(iMailboxHandle pHandle, bool pAndUnselect)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ExpungeAsync));
            return ZExpungeAsync(pHandle, pAndUnselect, lContext);
        }

        private async Task ZExpungeAsync(iMailboxHandle pHandle, bool pAndUnselect, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZExpungeAsync), pHandle, pAndUnselect);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                if (pAndUnselect) await lSession.CloseAsync(lMC, pHandle, lContext).ConfigureAwait(false);
                else await lSession.ExpungeAsync(lMC, pHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}