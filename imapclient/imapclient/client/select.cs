using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Select(cMailboxId pMailboxId, bool pForUpdate)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Select));
            var lTask = ZSelectAsync(pMailboxId, pForUpdate, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task SelectAsync(cMailboxId pMailboxId, bool pForUpdate)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(SelectAsync));
            return ZSelectAsync(pMailboxId, pForUpdate, lContext);
        }

        private async Task ZSelectAsync(cMailboxId pMailboxId, bool pForUpdate, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSelectAsync), pMailboxId, pForUpdate);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(Timeout, CancellationToken);
                await lSession.SelectAsync(lMC, pMailboxId, pForUpdate, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}