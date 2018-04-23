using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kAuthenticateCommandPartAuthenticate = new cTextCommandPart("AUTHENTICATE ");
            private static readonly cCommandPart kAuthenticateCommandPartEqual = new cTextCommandPart("=");

            public class cAuthenticateFailureDetails
            {
                public readonly cSASLAuthentication Authentication;
                public readonly Exception Exception;

                public cAuthenticateFailureDetails(cSASLAuthentication pAuthentication, Exception pException)
                {
                    Authentication = pAuthentication ?? throw new ArgumentNullException(nameof(pAuthentication));
                    Exception = pException;
                }
            }

            public async Task<cAuthenticateFailureDetails> AuthenticateAsync(cMethodControl pMC, string pHost, cSASL pSASL, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(AuthenticateAsync), pMC, pHost, pSASL.MechanismName);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notauthenticated) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotUnauthenticated);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kAuthenticateCommandPartAuthenticate);
                    lBuilder.Add(new cTextCommandPart(pSASL.MechanismName));

                    var lAuthentication = pSASL.GetAuthentication();
                    lBuilder.Add(lAuthentication);

                    if (_Capabilities.SASL_IR)
                    {
                        IList<byte> lAuthenticationResponse;

                        try { lAuthenticationResponse = lAuthentication.GetResponse(null); }
                        catch (Exception e)
                        {
                            lContext.TraceException("SASL authentication object threw when getting initial response", e);
                            return new cAuthenticateFailureDetails(lAuthentication, e);
                        }

                        if (lAuthenticationResponse != null)
                        {
                            lBuilder.Add(cCommandPart.Space);
                            if (lAuthenticationResponse.Count == 0) lBuilder.Add(kAuthenticateCommandPartEqual); // special case where the initial response is an empty string
                            else lBuilder.Add(new cTextCommandPart(cBase64.Encode(lAuthenticationResponse), true));
                        }
                    }

                    var lHook = new cCommandHookAuthenticate(mPipeline, lAuthentication, _Capabilities.LoginReferrals);
                    lBuilder.Add(lHook);

                    var lCapabilities = mPipeline.Capabilities;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("authenticate success");
                        ZAuthenticated(lCapabilities, lHook, lResult.ResponseText, new cAccountId(pHost, pSASL.CredentialId), lContext);
                        return null;
                    }

                    if (lResult.ResultType == eIMAPCommandResultType.no)
                    {
                        lContext.TraceInformation("authenticate failed: {0}", lResult.ResponseText);

                        if (ZSetHomeServerReferral(lResult.ResponseText, lContext)) return new cAuthenticateFailureDetails(lAuthentication, new cIMAPHomeServerReferralException(lResult.ResponseText, lContext));

                        if (lResult.ResponseText.Code == eIMAPResponseTextCode.authenticationfailed || lResult.ResponseText.Code == eIMAPResponseTextCode.authorizationfailed || lResult.ResponseText.Code == eIMAPResponseTextCode.expired)
                            return new cAuthenticateFailureDetails(lAuthentication, new cIMAPCredentialsException(lResult.ResponseText, lContext));

                        return new cAuthenticateFailureDetails(lAuthentication, null);
                    }

                    lContext.TraceInformation("authenticate cancelled");

                    return new cAuthenticateFailureDetails(lAuthentication, null);
                }
            }

            private class cCommandHookAuthenticate : cCommandHookInitial
            {
                private readonly cCommandPipeline mPipeline;
                private readonly cSASLAuthentication mAuthentication;

                public cCommandHookAuthenticate(cCommandPipeline pPipeline, cSASLAuthentication pAuthentication, bool pHandleReferral)
                {
                    mPipeline = pPipeline;
                    mAuthentication = pAuthentication;
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookAuthenticate), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eIMAPCommandResultType.ok) return;
                    var lSecurity = mAuthentication.GetSecurity();
                    if (lSecurity != null) mPipeline.InstallSASLSecurity(lSecurity, lContext);
                }
            }
        }
    }
}