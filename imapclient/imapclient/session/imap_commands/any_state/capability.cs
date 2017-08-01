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
            private static readonly cCommandPart kCapabilityCommandPart = new cCommandPart("CAPABILITY");

            public async Task CapabilityAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CapabilityAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notauthenticated && _State != eState.authenticated) throw new InvalidOperationException();

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kCapabilityCommandPart);

                    var lHook = new cCapabilityCommandHook();
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("capability success");

                        if (lHook.Capabilities != null) ZSetCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, lContext);
                        else throw new cUnexpectedServerActionException(0, "capability not received", lContext);

                        return;
                    }

                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a failed capability");

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cCapabilityCommandHook : cCommandHook
            {
                private static readonly cBytes kCapabilitySpace = new cBytes("CAPABILITY ");

                private cCapabilities mCapabilities = null;
                private cCapabilities mAuthenticationMechanisms = null;

                public cCapabilityCommandHook() { }

                public cCapabilities Capabilities => mCapabilities;
                public cCapabilities AuthenticationMechanisms => mAuthenticationMechanisms;

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCapabilityCommandHook), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kCapabilitySpace)) return eProcessDataResult.notprocessed;

                    if (pCursor.ProcessCapability(out mCapabilities, out mAuthenticationMechanisms, lContext) && pCursor.Position.AtEnd)
                    {
                        lContext.TraceVerbose("got capabilities: {0} {1}", mCapabilities, mAuthenticationMechanisms);
                        return eProcessDataResult.processed;
                    }

                    lContext.TraceWarning("likely malformed capability");
                    return eProcessDataResult.notprocessed;
                }
            }
        }
    }
}