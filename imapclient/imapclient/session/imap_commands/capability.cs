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

                // note that the permanent capability response data processor was installed when the session was constructed

                object lCapability = _Capability;

                using (var lCommand = new cCommand())
                {
                    // note the lack of locking - this is only called during connect

                    lCommand.Add(kCapabilityCommandPart);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("capability success");
                        if (ReferenceEquals(_Capability, lCapability)) throw new cUnexpectedServerActionException(0, "capability not received", lContext);
                        return;
                    }

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}