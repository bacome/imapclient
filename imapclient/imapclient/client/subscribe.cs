using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal void Subscribe(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Subscribe));
            var lTask = ZSubscribeAsync(pMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task SubscribeAsync(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SubscribeAsync));
            return ZSubscribeAsync(pMailboxHandle, lContext);
        }

        private async Task ZSubscribeAsync(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSubscribeAsync), pMailboxHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                await lSession.SubscribeAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
            }
        }

        internal void Unsubscribe(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Unsubscribe));
            var lTask = ZUnsubscribeAsync(pMailboxHandle, lContext);
            mSynchroniser.Wait(lTask, lContext);
        }

        internal Task UnsubscribeAsync(iMailboxHandle pMailboxHandle)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UnsubscribeAsync));
            return ZUnsubscribeAsync(pMailboxHandle, lContext);
        }

        private async Task ZUnsubscribeAsync(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUnsubscribeAsync), pMailboxHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                await lSession.UnsubscribeAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
            }
        }
    }
}