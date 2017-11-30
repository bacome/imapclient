using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cMailbox Rename(iMailboxHandle pMailboxHandle, cMailboxName pMailboxName)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Rename));
            var lTask = ZRenameAsync(pMailboxHandle, pMailboxName, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cMailbox> RenameAsync(iMailboxHandle pMailboxHandle, cMailboxName pMailboxName)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(RenameAsync));
            return ZRenameAsync(pMailboxHandle, pMailboxName, lContext);
        }

        private async Task<cMailbox> ZRenameAsync(iMailboxHandle pMailboxHandle, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZRenameAsync), pMailboxHandle, pMailboxName);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                var lMailboxHandle = await lSession.RenameAsync(lMC, pMailboxHandle, pMailboxName, lContext).ConfigureAwait(false);
                return new cMailbox(this, lMailboxHandle);
            }
        }
    }
}