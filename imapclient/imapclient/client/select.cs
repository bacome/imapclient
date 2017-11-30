using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Select(iMailboxHandle pMailboxHandle, bool pForUpdate)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Select));
            var lTask = ZSelectAsync(pMailboxHandle, pForUpdate, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task SelectAsync(iMailboxHandle pMailboxHandle, bool pForUpdate)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SelectAsync));
            return ZSelectAsync(pMailboxHandle, pForUpdate, lContext);
        }

        private async Task ZSelectAsync(iMailboxHandle pMailboxHandle, bool pForUpdate, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSelectAsync), pMailboxHandle, pForUpdate);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                if (pForUpdate) await lSession.SelectAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                else await lSession.ExamineAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}