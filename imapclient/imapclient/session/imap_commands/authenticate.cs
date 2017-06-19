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

                using (var lCommand = new cCommand())
                {
                    //  note the lack of locking - this is only called during connect

                    var lCapability = _Capability;

                    lCommand.Add(kAuthenticateCommandPartAuthenticate);
                    lCommand.Add(new cCommandPart(pSASL.MechanismName));

                    var lAuthentication = pSASL.GetAuthentication();

                    if (lCapability.SASL_IR)
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
                            lCommand.Add(cCommandPart.Space);
                            if (lAuthenticationResponse.Count == 0) lCommand.Add(kAuthenticateCommandPartEqual); // special case where the initial response is an empty string
                            else lCommand.Add(new cCommandPart(cBase64.Encode(lAuthenticationResponse), true));
                        }
                    }

                    lCommand.Add(lAuthentication);

                    var lHook = new cCommandHookInitial(lCapability.LoginReferrals);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("authenticate success");

                        var lSecurity = lAuthentication.GetSecurity();
                        if (lSecurity != null) mConnection.InstallSecurity(lSecurity, lContext);

                        if (lHook.Capabilities != null) ZSetCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, lContext);
                        if (lHook.HomeServerReferral != null) ZSetHomeServerReferral(new cURL(lHook.HomeServerReferral), lContext);
                        ZSetConnectedAccountId(pAccountId, lContext);

                        return null;
                    }

                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a failed authenticate");

                    if (lResult.Result == cCommandResult.eResult.no)
                    {
                        lContext.TraceInformation("authenticate failed: {0}", lResult.ResponseText);

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

                    lContext.TraceInformation("authenticate cancelled");
                    if (lHook.HomeServerReferral != null) lContext.TraceError("received a referral on a cancelled authenticate");

                    return null;
                }
            }
        }
    }
}