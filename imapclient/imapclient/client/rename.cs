using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cMailbox Rename(iMailboxHandle pHandle, cMailboxName pMailboxName)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Rename));
            var lTask = ZRenameAsync(pHandle, pMailboxName, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cMailbox> RenameAsync(iMailboxHandle pHandle, cMailboxName pMailboxName)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(RenameAsync));
            return ZRenameAsync(pHandle, pMailboxName, lContext);
        }

        private async Task<cMailbox> ZRenameAsync(iMailboxHandle pHandle, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZRenameAsync), pHandle, pMailboxName);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                var lHandle = await lSession.RenameAsync(lMC, pHandle, pMailboxName, lContext).ConfigureAwait(false);
                return new cMailbox(this, lHandle);
            }
        }
    }
}