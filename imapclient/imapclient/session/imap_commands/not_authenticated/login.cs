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
            private static readonly cCommandPart kLoginCommandPartLogin = new cTextCommandPart("LOGIN ");

            public async Task<Exception> LoginAsync(cMethodControl pMC, cAccountId pAccountId, cLogin pLogin, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(LoginAsync), pMC, pAccountId);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notauthenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnauthenticated);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kLoginCommandPartLogin, cCommandPartFactory.AsASCIILiteral(pLogin.UserId), cCommandPart.Space, cCommandPartFactory.AsASCIILiteral(pLogin.Password));

                    var lHook = new cCommandHookInitial();
                    lBuilder.Add(lHook);

                    var lCapabilities = mPipeline.Capabilities;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("login success");
                        ZAuthenticated(lCapabilities, lHook, lResult.ResponseText, pAccountId, lContext);
                        return null;
                    }

                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a failed login");

                    if (lResult.ResultType == eCommandResultType.no)
                    {
                        lContext.TraceInformation("login failed: {0}", lResult.ResponseText);

                        if (ZSetHomeServerReferral(lResult.ResponseText, lContext)) return new cHomeServerReferralException(lResult.ResponseText, lContext);

                        if (lResult.ResponseText.Code == eResponseTextCode.authenticationfailed || lResult.ResponseText.Code == eResponseTextCode.authorizationfailed || lResult.ResponseText.Code == eResponseTextCode.expired)
                            return new cCredentialsException(lResult.ResponseText, lContext);

                        return null;
                    }

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}