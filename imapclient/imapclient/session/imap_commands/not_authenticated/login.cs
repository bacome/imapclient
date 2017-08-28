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
                if (_ConnectionState != eConnectionState.notauthenticated) throw new InvalidOperationException();

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kLoginCommandPartLogin, mCommandPartFactory.AsLiteral(pLogin.UserId), cCommandPart.Space, mCommandPartFactory.AsLiteral(pLogin.Password));

                    var lHook = new cCommandHookInitial();
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("login success");

                        if (lHook.Capabilities != null)
                        {
                            mCapabilities = new cCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, mIgnoreCapabilities);
                            mPipeline.SetCapability(mCapabilities, lContext);
                            mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
                        }

                        ZSetHomeServerReferral(lResult.ResponseText, lContext);
                        ZSetConnectedAccountId(pAccountId, lContext);

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