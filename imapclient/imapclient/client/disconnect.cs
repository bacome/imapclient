using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public void Disconnect()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Disconnect));
            mEventSynchroniser.Wait(ZDisconnectAsync(lContext), lContext);
        }

        public Task DisconnectAsync()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(DisconnectAsync));
            return ZDisconnectAsync(lContext);
        }

        private async Task ZDisconnectAsync(cTrace.cContext pParentContext)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ZDisconnectAsync));

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            mAsyncCounter.Increment(lContext);

            try
            {
                var lMC = new cMethodControl(mTimeout, CancellationToken);
                await lSession.LogoutAsync(lMC, lContext).ConfigureAwait(false);
            }
            catch when (lSession.State != eState.disconnected)
            {           
                lSession.Disconnect(lContext);
                throw;
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }
    }
}