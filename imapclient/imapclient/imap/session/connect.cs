using System;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task ConnectAsync(cMethodControl pMC, cServiceId pServiceId, object pPreAuthenticatedCredentialId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ConnectAsync), pMC, pServiceId, pPreAuthenticatedCredentialId);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (_ConnectionState != eIMAPConnectionState.notconnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnconnected);
                ZSetState(eIMAPConnectionState.connecting, lContext);

                sGreeting lGreeting;

                try { lGreeting = await mPipeline.ConnectAsync(pMC, pServiceId, lContext).ConfigureAwait(false); }
                catch (Exception)
                {
                    ZSetState(eIMAPConnectionState.disconnected, lContext);
                    throw;
                }

                if (lGreeting.Type == eGreetingType.bye)
                {
                    ZSetState(eIMAPConnectionState.disconnected, lContext);
                    if (ZSetHomeServerReferral(lGreeting.ResponseText, lContext)) throw new cIMAPHomeServerReferralException(lGreeting.ResponseText, lContext);
                    throw new cConnectByeException(lGreeting.ResponseText, lContext);
                }

                if (mPipeline.Capabilities != null) ZSetCapabilities(mPipeline.Capabilities, mPipeline.AuthenticationMechanisms, lContext);

                if (lGreeting.Type == eGreetingType.ok)
                {
                    ZSetState(eIMAPConnectionState.notauthenticated, lContext);
                    return;
                }

                // preauth

                ZSetHomeServerReferral(lGreeting.ResponseText, lContext);

                if (pPreAuthenticatedCredentialId == null) throw new cUnexpectedPreAuthenticatedConnectionException(lContext);
                else ZSetConnectedAccountId(new cAccountId(pServiceId.Host, pPreAuthenticatedCredentialId), lContext);
            }
        } 
    }
}
