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
            mSynchroniser.Wait(ZDisconnectAsync(lContext), lContext);
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
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException();

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);

                try { await lSession.LogoutAsync(lMC, lContext).ConfigureAwait(false); }
                catch when (lSession.ConnectionState != eConnectionState.disconnected)
                {
                    lSession.Disconnect(lContext);
                    throw;
                }
            }
        }
    }
}