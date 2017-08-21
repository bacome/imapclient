using System;
using System.Collections.Generic;
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
            private static readonly cCommandPart kAuthenticateCommandPartAuthenticate = new cCommandPart("AUTHENTICATE ");
            private static readonly cCommandPart kAuthenticateCommandPartEqual = new cCommandPart("=");

            public async Task<Exception> AuthenticateAsync(cMethodControl pMC, cAccountId pAccountId, cSASL pSASL, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(AuthenticateAsync), pMC, pAccountId, pSASL.MechanismName);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notauthenticated) throw new InvalidOperationException();

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kAuthenticateCommandPartAuthenticate);
                    lBuilder.Add(new cCommandPart(pSASL.MechanismName));

                    var lAuthentication = pSASL.GetAuthentication();

                    if (mCapabilities.SASL_IR)
                    {
                        IList<byte> lAuthenticationResponse;

                        try { lAuthenticationResponse = lAuthentication.GetResponse(null); }
                        catch (Exception e)
                        {
                            lContext.TraceException("SASL authentication object threw when getting initial response", e);
                            return null;
                        }

                        if (lAuthenticationResponse != null)
                        {
                            lBuilder.Add(cCommandPart.Space);
                            if (lAuthenticationResponse.Count == 0) lBuilder.Add(kAuthenticateCommandPartEqual); // special case where the initial response is an empty string
                            else lBuilder.Add(new cCommandPart(cBase64.Encode(lAuthenticationResponse), eCommandPartType.text, true));
                        }
                    }

                    lBuilder.Add(lAuthentication);

                    var lHook = new cCommandHookAuthenticate(mConnection, lAuthentication, mCapabilities.LoginReferrals);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("authenticate success");

                        if (lHook.Capabilities != null)
                        {
                            mCapabilities = new cCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, mIgnoreCapabilities);
                            mPipeline.SetCapability(mCapabilities, lContext);
                            mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
                        }

                        ZSetHomeServerReferral(lResult.ResponseText, lContext);
                        ZSetConnectedAccountId(pAccountId, lContext);

                        return null;
                    }

                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a failed authenticate");

                    if (lResult.ResultType == eCommandResultType.no)
                    {
                        lContext.TraceInformation("authenticate failed: {0}", lResult.ResponseText);

                        if (ZSetHomeServerReferral(lResult.ResponseText, lContext)) return new cHomeServerReferralException(lResult.ResponseText, lContext);

                        if (lResult.ResponseText.Code == eResponseTextCode.authenticationfailed || lResult.ResponseText.Code == eResponseTextCode.authorizationfailed || lResult.ResponseText.Code == eResponseTextCode.expired)
                            return new cCredentialsException(lResult.ResponseText, lContext);

                        return null;
                    }

                    lContext.TraceInformation("authenticate cancelled");

                    return null;
                }
            }

            private class cCommandHookAuthenticate : cCommandHookInitial
            {
                private readonly cConnection mConnection;
                private readonly cSASLAuthentication mAuthentication;

                public cCommandHookAuthenticate(cConnection pConnection, cSASLAuthentication pAuthentication, bool pHandleReferral)
                {
                    mConnection = pConnection;
                    mAuthentication = pAuthentication;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookAuthenticate), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eCommandResultType.ok) return;
                    var lSecurity = mAuthentication.GetSecurity();
                    if (lSecurity != null) mConnection.InstallSASLSecurity(lSecurity, lContext);
                }
            }
        }
    }
}