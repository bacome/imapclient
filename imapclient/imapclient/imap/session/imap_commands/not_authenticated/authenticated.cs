using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private void ZAuthenticated(cStrings pOldPipelineCapabilities, cCommandHookInitial pHook, cIMAPResponseText pResponseText, cAccountId pAccountId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZAuthenticated));

                if (pHook.Capabilities != null) ZSetCapabilities(pHook.Capabilities, pHook.AuthenticationMechanisms, lContext);
                else if (!ReferenceEquals(pOldPipelineCapabilities, mPipeline.Capabilities)) ZSetCapabilities(mPipeline.Capabilities, mPipeline.AuthenticationMechanisms, lContext);

                ZSetHomeServerReferral(pResponseText, lContext);
                ZSetConnectedAccountId(pAccountId, lContext);
            }
        }
    }
}