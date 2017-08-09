using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Poll()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Poll));
            mEventSynchroniser.Wait(ZPollAsync(lContext), lContext);
        }

        public Task PollAsync()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(PollAsync));
            return ZPollAsync(lContext);
        }

        private async Task ZPollAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZPollAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            mAsyncCounter.Increment(lContext);
            
            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);
                var lCheck = lSession.CheckAsync(lMC, lContext);
                var lNoOp = lSession.NoOpAsync(lMC, lContext);
                await Task.WhenAll(lCheck, lNoOp).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}