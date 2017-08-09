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
                if (_State != eState.notauthenticated) throw new InvalidOperationException();

                using (var lCommand = new cCommand())
                {
                    //  note the lack of locking - this is only called during connect

                    lCommand.Add(kAuthenticateCommandPartAuthenticate);
                    lCommand.Add(new cCommandPart(pSASL.MechanismName));

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
                            lCommand.Add(cCommandPart.Space);
                            if (lAuthenticationResponse.Count == 0) lCommand.Add(kAuthenticateCommandPartEqual); // special case where the initial response is an empty string
                            else lCommand.Add(new cCommandPart(cBase64.Encode(lAuthenticationResponse), eCommandPartType.text, true));
                        }
                    }

                    lCommand.Add(lAuthentication);

                    var lHook = new cCommandHookAuthenticate(mConnection, lAuthentication, mCapabilities.LoginReferrals);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("authenticate success");

                        if (lHook.Capabilities != null) mCapabilities = new cCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, mIgnoreCapabilities);
                        if (lHook.HomeServerReferral != null) mHomeServerReferral = new cURL(lHook.HomeServerReferral);
                        ZSetConnectedAccountId(pAccountId, lContext);

                        return null;
                    }

                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a failed authenticate");

                    if (lResult.ResultType == eCommandResultType.no)
                    {
                        lContext.TraceInformation("authenticate failed: {0}", lResult.ResponseText);

                        if (lHook.HomeServerReferral != null)
                        {
                            mHomeServerReferral = new cURL(lHook.HomeServerReferral);
                            return new cHomeServerReferralException(mHomeServerReferral, lResult.ResponseText, lContext);
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

            private class cCommandHookAuthenticate : cCommandHookInitial
            {
                private readonly cConnection mConnection;
                private readonly cSASLAuthentication mAuthentication;

                public cCommandHookAuthenticate(cConnection pConnection, cSASLAuthentication pAuthentication, bool pHandleReferral) : base(pHandleReferral)
                {
                    mConnection = pConnection;
                    mAuthentication = pAuthentication;
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookAuthenticate), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok)
                    {
                        var lSecurity = mAuthentication.GetSecurity();
                        if (lSecurity != null) mConnection.InstallSASLSecurity(lSecurity, lContext);
                    }
                }
            }
        }
    }
}