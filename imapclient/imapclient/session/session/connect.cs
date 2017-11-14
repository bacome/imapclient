using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ConnectAsync), pMC, pServer);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (_ConnectionState != eConnectionState.notconnected) throw new InvalidOperationException();
                ZSetState(eConnectionState.connecting, lContext);

                sGreeting lGreeting;

                try { lGreeting = await mPipeline.ConnectAsync(pMC, pServer, lContext).ConfigureAwait(false); }
                catch (Exception)
                {
                    ZSetState(eConnectionState.disconnected, lContext);
                    throw;
                }

                if (lGreeting.Type == eGreetingType.bye)
                {
                    ZSetState(eConnectionState.disconnected, lContext);
                    if (ZSetHomeServerReferral(lGreeting.ResponseText, lContext)) throw new cHomeServerReferralException(lGreeting.ResponseText, lContext);
                    throw new cConnectByeException(lGreeting.ResponseText, lContext);
                }

                if (lGreeting.Capabilities != null)
                {
                    mCapabilities = new cCapabilities(lGreeting.Capabilities, lGreeting.AuthenticationMechanisms, mIgnoreCapabilities);
                    mPipeline.SetCapabilities(mCapabilities, lContext);
                    mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
                }

                if (lGreeting.Type == eGreetingType.ok)
                {
                    ZSetState(eConnectionState.notauthenticated, lContext);
                    return;
                }

                // preauth

                ZSetHomeServerReferral(lGreeting.ResponseText, lContext);
                ZSetConnectedAccountId(new cAccountId(pServer.Host, eAccountType.unknown), lContext);
            }
        } 
    }
}
