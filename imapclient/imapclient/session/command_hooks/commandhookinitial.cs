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

                public cCommandHookInitial() { }

                public cStrings Capabilities { get; private set; } = null;
                public cStrings AuthenticationMechanisms { get; private set; } = null;

                public override bool ProcessTextCode(eResponseTextType pTextType, cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookInitial), nameof(ProcessTextCode), pTextType);

                    if (pTextType == eResponseTextType.greeting || pTextType == eResponseTextType.success)
                    {
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
                    }

                    return false;
                }
            }
        }
    }
}