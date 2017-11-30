using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cMessageHandleList SetUnseenCount(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZSetUnseenCountAsync(pMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cMessageHandleList> SetUnseenCountAsync(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZSetUnseenCountAsync(pMailboxHandle, lContext);
        }

        private async Task<cMessageHandleList> ZSetUnseenCountAsync(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesAsync), pMailboxHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            var lCapabilities = lSession.Capabilities;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                if (lCapabilities.ESearch) return await lSession.SetUnseenCountExtendedAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                else return await lSession.SetUnseenCountAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}