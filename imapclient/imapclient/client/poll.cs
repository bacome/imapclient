using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Solicits pending notifications from the server using IMAP CHECK (if a mailbox is selected) and IMAP NOOP.
        /// </summary>
        public void Poll()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Poll));
            mSynchroniser.Wait(ZPollAsync(lContext), lContext);
        }

        /// <summary>
        /// Asynchronously solicits pending notifications from the server using IMAP CHECK (if a mailbox is selected) and IMAP NOOP.
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
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

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