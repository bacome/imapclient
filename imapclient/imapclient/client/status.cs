using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public int StatusCacheAgeMax { get; set; } = 5000;

        public cMailboxStatus Status(cMailboxId pMailboxId, int? pCacheAgeMax = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Status));
            var lTask = ZStatusAsync(pMailboxId, pCacheAgeMax ?? StatusCacheAgeMax, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<cMailboxStatus> StatusAsync(cMailboxId pMailboxId, int? pCacheAgeMax = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(StatusAsync));
            return ZStatusAsync(pMailboxId, pCacheAgeMax ?? StatusCacheAgeMax, lContext);
        }

        private async Task<cMailboxStatus> ZStatusAsync(cMailboxId pMailboxId, int pCacheAgeMax, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZStatusAsync), pMailboxId, pCacheAgeMax);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);
                return await lSession.StatusAsync(lMC, pMailboxId, pCacheAgeMax, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}