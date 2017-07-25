using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kLoginCommandPartLogin = new cCommandPart("LOGIN ");

            public async Task<Exception> LoginAsync(cMethodControl pMC, cAccountId pAccountId, cLogin pLogin, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(LoginAsync), pMC, pAccountId);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notauthenticated) throw new InvalidOperationException();

                using (var lCommand = new cCommand())
                {
                    //  note the lack of locking - this is only called during connect

                    lCommand.Add(kLoginCommandPartLogin, cCommandPartFactory.AsLiteral(pLogin.UserId), cCommandPart.Space, lFactory.AsLiteral(pLogin.Password));

                    var lHook = new cCommandHookInitial(_Capability.LoginReferrals);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("login success");

                        if (lHook.Capabilities != null) ZSetCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, lContext);
                        if (lHook.HomeServerReferral != null) ZSetHomeServerReferral(new cURL(lHook.HomeServerReferral), lContext);
                        ZSetConnectedAccountId(pAccountId, lContext);

                        return null;
                    }

                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a failed login");

                    if (lResult.Result == cCommandResult.eResult.no)
                    {
                        lContext.TraceInformation("login failed: {0}", lResult.ResponseText);

                        if (lHook.HomeServerReferral != null)
                        {
                            cURL lURL = new cURL(lHook.HomeServerReferral);
                            ZSetHomeServerReferral(lURL, lContext);
                            return new cHomeServerReferralException(lURL, lResult.ResponseText, lContext);
                        }

                        if (lResult.ResponseText.Code == eResponseTextCode.authenticationfailed || lResult.ResponseText.Code == eResponseTextCode.authorizationfailed || lResult.ResponseText.Code == eResponseTextCode.expired)
                            return new cCredentialsException(lResult.ResponseText, lContext);

                        return null;
                    }

                    if (lHook.HomeServerReferral != null) lContext.TraceError("received a referral on an unrecognised login");

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}