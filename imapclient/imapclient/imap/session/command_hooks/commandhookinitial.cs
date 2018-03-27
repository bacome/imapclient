﻿using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookInitial : cCommandHook
            {
                private static readonly cBytes kCapability = new cBytes("CAPABILITY");
                
                public cCommandHookInitial() { }

                public cStrings Capabilities { get; private set; } = null;
                public cStrings AuthenticationMechanisms { get; private set; } = null;

                public override void ProcessTextCode(eIMAPResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookInitial), nameof(ProcessTextCode), pTextContext, pCode, pArguments);

                    if (pTextContext == eIMAPResponseTextContext.greetingok || pTextContext == eIMAPResponseTextContext.greetingpreauth || pTextContext == eIMAPResponseTextContext.success)
                    {
                        if (pCode.Equals(kCapability))
                        {
                            if (pArguments != null)
                            {
                                cBytesCursor lCursor = new cBytesCursor(pArguments);

                                if (lCursor.ProcessCapability(out var lCapabilities, out var lAuthenticationMechanisms, lContext) && lCursor.Position.AtEnd)
                                {
                                    lContext.TraceVerbose("received capabilities: {0} {1}", lCapabilities, lAuthenticationMechanisms);
                                    Capabilities = lCapabilities;
                                    AuthenticationMechanisms = lAuthenticationMechanisms;
                                    return;
                                }
                            }

                            lContext.TraceWarning("likely malformed capability response");
                        }
                    }
                }
            }
        }
    }
}