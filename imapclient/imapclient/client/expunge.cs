using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Expunge(iMailboxHandle pHandle, bool pAndClose)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Expunge));
            var lTask = ZExpungeAsync(pHandle, pAndClose, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task ExpungeAsync(iMailboxHandle pHandle, bool pAndClose)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ExpungeAsync));
            return ZExpungeAsync(pHandle, pAndClose, lContext);
        }

        private async Task ZExpungeAsync(iMailboxHandle pHandle, bool pAndClose, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZExpungeAsync), pHandle, pAndClose);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);
                if (pAndClose) await lSession.CloseAsync(lMC, pHandle, lContext).ConfigureAwait(false);
                else await lSession.ExpungeAsync(lMC, pHandle, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}