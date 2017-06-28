﻿using System;
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
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            mAsyncCounter.Increment(lContext);
            
            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);
                await lSession.PollAsync(lMC, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}