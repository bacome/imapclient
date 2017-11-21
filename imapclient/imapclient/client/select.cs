using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Select(iMailboxHandle pHandle, bool pForUpdate)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Select));
            var lTask = ZSelectAsync(pHandle, pForUpdate, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task SelectAsync(iMailboxHandle pHandle, bool pForUpdate)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SelectAsync));
            return ZSelectAsync(pHandle, pForUpdate, lContext);
        }

        private async Task ZSelectAsync(iMailboxHandle pHandle, bool pForUpdate, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSelectAsync), pHandle, pForUpdate);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                if (pForUpdate) await lSession.SelectAsync(lMC, pHandle, lContext).ConfigureAwait(false);
                else await lSession.ExamineAsync(lMC, pHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}