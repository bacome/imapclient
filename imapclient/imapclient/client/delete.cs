using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Delete(iMailboxHandle pHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Delete));
            var lTask = ZDeleteAsync(pHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task DeleteAsync(iMailboxHandle pHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(DeleteAsync));
            return ZDeleteAsync(pHandle, lContext);
        }

        private async Task ZDeleteAsync(iMailboxHandle pHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZDeleteAsync), pHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                await lSession.DeleteAsync(lMC, pHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}