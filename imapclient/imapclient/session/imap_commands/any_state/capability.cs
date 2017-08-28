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
                if (_ConnectionState != eConnectionState.notauthenticated && _ConnectionState != eConnectionState.authenticated) throw new InvalidOperationException();

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    //  note the lack of locking - this is only called during connect

                    lBuilder.Add(kCapabilityCommandPart);

                    var lHook = new cCapabilityCommandHook();
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("capability success");

                        if (lHook.Capabilities != null)
                        {
                            mCapabilities = new cCapabilities(lHook.Capabilities, lHook.AuthenticationMechanisms, mIgnoreCapabilities);
                            mPipeline.SetCapability(mCapabilities, lContext);
                            mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.Capabilities), lContext);
                        }
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

                public cCapabilityCommandHook() { }

                public cUniqueIgnoreCaseStringList Capabilities { get; private set; } = null;
                public cUniqueIgnoreCaseStringList AuthenticationMechanisms { get; private set; } = null;

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCapabilityCommandHook), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kCapabilitySpace)) return eProcessDataResult.notprocessed;

                    if (pCursor.ProcessCapability(out var lCapabilities, out var lAuthenticationMechanisms, lContext) && pCursor.Position.AtEnd)
                    {
                        lContext.TraceVerbose("got capabilities: {0} {1}", lCapabilities, lAuthenticationMechanisms);
                        Capabilities = lCapabilities;
                        AuthenticationMechanisms = lAuthenticationMechanisms;
                        return eProcessDataResult.processed;
                    }

                    lContext.TraceWarning("likely malformed capability");
                    return eProcessDataResult.notprocessed;
                }
            }
        }
    }
}