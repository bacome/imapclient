using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cMailboxStatus Status(iMailboxHandle pHandle)
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

        private async Task<cMailboxStatus> ZStatusAsync(iMailboxHandle pHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZStatusAsync), pHandle);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);
                return await lSession.StatusAsync(lMC, pHandle, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}