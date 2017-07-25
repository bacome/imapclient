using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookInitial : cCommandHook
            {
                private static readonly cBytes kCapabilitySpace = new cBytes("CAPABILITY ");
                private static readonly cBytes kReferralSpace = new cBytes("REFERRAL ");

                private readonly bool mHandleReferral;

                private cCapabilities mCapabilities = null;
                private cCapabilities mAuthenticationMechanisms = null;

                public string HomeServerReferral { get; private set; } = null;

                public cCommandHookInitial(bool pHandleReferral)
                {
                    mHandleReferral = pHandleReferral;
                }

                public cCapabilities Capabilities => mCapabilities;
                public cCapabilities AuthenticationMechanisms => mAuthenticationMechanisms;

                public override bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookInitial), nameof(ProcessTextCode));

                    if (pCursor.SkipBytes(kCapabilitySpace))
                    {
                        if (pCursor.ProcessCapability(out mCapabilities, out mAuthenticationMechanisms, lContext) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("received capabilities: {0} {1}", mCapabilities, mAuthenticationMechanisms);
                            return true;
                        }

                        lContext.TraceWarning("likely malformed capability response");
                    }
                    else if (mHandleReferral && pCursor.SkipBytes(kReferralSpace))
                    {
                        if (pCursor.GetURL(out var lParts, out var lURL, lContext) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("received referral {0}", lURL);

                            if (lParts.IsHomeServerReferral)
                            {
                                HomeServerReferral = lURL;
                                return true;
                            }
                        }

                        lContext.TraceWarning("likely malformed referral response");
                    }

                    return false;
                }
            }
        }
    }
}