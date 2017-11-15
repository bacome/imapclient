using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Disconnects gracefully from the connected server.
        /// Can only be called when the instance <see cref="IsConnected"/>.
        /// </summary>
        /// <remarks>
        /// Long running operations that are in progress will fail.
        /// (The gracefully refers to the IMAP protocol.)
        /// </remarks>
        public void Disconnect()
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Disconnect));
            mSynchroniser.Wait(ZDisconnectAsync(lContext), lContext);
        }

        /// <summary>
        /// Disconnects gracefully and asynchronously from the connected server.
        /// Can only be called when the instance <see cref="IsConnected"/>.
        /// </summary>
        /// <returns></returns>
        /// <inheritdoc cref="Disconnect" select="remarks"/>
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