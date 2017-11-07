﻿using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Poll the server using IMAP CHECK (if a mailbox is selected) and IMAP NOOP to see if the server has pending notifications for us.
        /// </summary>
        public void Poll()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Poll));
            mSynchroniser.Wait(ZPollAsync(lContext), lContext);
        }

        /// <summary>
        /// Poll the server using IMAP CHECK (if a mailbox is selected) and IMAP NOOP to see if the server has pending notifications for us.
        /// </summary>
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

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                var lCheck = lSession.CheckAsync(lMC, lContext);
                var lNoOp = lSession.NoOpAsync(lMC, lContext);
                await Task.WhenAll(lCheck, lNoOp).ConfigureAwait(false);
            }
        }
    }
}