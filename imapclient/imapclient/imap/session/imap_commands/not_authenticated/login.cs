using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kLoginCommandPartLogin = new cTextCommandPart("LOGIN ");

            public async Task<Exception> LoginAsync(cMethodControl pMC, string pHost, cIMAPLogin pLogin, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(LoginAsync), pMC, pHost);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notauthenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnauthenticated);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kLoginCommandPartLogin, cCommandPartFactory.AsASCIILiteral(pLogin.UserId), cCommandPart.Space, cCommandPartFactory.AsASCIILiteral(pLogin.Password));

                    var lHook = new cCommandHookInitial();
                    lBuilder.Add(lHook);

                    var lCapabilities = mPipeline.Capabilities;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("login success");
                        ZAuthenticated(lCapabilities, lHook, lResult.ResponseText, new cAccountId(pHost, pLogin.UserId), lContext);
                        return null;
                    }

                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a failed login");

                    if (lResult.ResultType == eIMAPCommandResultType.no)
                    {
                        lContext.TraceInformation("login failed: {0}", lResult.ResponseText);

                        if (ZSetHomeServerReferral(lResult.ResponseText, lContext)) return new cIMAPHomeServerReferralException(lResult.ResponseText, lContext);

                        if (lResult.ResponseText.Code == eIMAPResponseTextCode.authenticationfailed || lResult.ResponseText.Code == eIMAPResponseTextCode.authorizationfailed || lResult.ResponseText.Code == eIMAPResponseTextCode.expired)
                            return new cIMAPCredentialsException(lResult.ResponseText, lContext);

                        return null;
                    }

                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}