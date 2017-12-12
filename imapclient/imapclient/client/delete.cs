using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Delete(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Delete));
            var lTask = ZDeleteAsync(pMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task DeleteAsync(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(DeleteAsync));
            return ZDeleteAsync(pMailboxHandle, lContext);
        }

        private async Task ZDeleteAsync(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZDeleteAsync), pMailboxHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                await lSession.DeleteAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}