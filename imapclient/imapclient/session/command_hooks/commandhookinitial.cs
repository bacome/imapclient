using System;
using System.Collections.Generic;
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

                public cCommandHookInitial(bool pHandleReferral)
                {
                    mHandleReferral = pHandleReferral;
                }

                public cUniqueIgnoreCaseStringList Capabilities { get; private set; } = null;
                public cUniqueIgnoreCaseStringList AuthenticationMechanisms { get; private set; } = null;
                public string HomeServerReferral { get; private set; } = null;

                public override bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookInitial), nameof(ProcessTextCode));

                    if (pCursor.SkipBytes(kCapabilitySpace))
                    {
                        if (pCursor.ProcessCapability(out var lCapabilities, out var lAuthenticationMechanisms, lContext) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lContext.TraceVerbose("received capabilities: {0} {1}", lCapabilities, lAuthenticationMechanisms);
                            Capabilities = lCapabilities;
                            AuthenticationMechanisms = lAuthenticationMechanisms;
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