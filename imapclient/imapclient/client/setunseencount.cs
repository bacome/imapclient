using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cMessageHandleList SetUnseenCount(iMailboxHandle pHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZSetUnseenCountAsync(pHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cMessageHandleList> SetUnseenCountAsync(iMailboxHandle pHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZSetUnseenCountAsync(pHandle, lContext);
        }

        private async Task<cMessageHandleList> ZSetUnseenCountAsync(iMailboxHandle pHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesAsync), pHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            var lCapabilities = lSession.Capabilities;

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                if (lCapabilities.ESearch) return await lSession.SetUnseenCountExtendedAsync(lMC, pHandle, lContext).ConfigureAwait(false);
                else return await lSession.SetUnseenCountAsync(lMC, pHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}