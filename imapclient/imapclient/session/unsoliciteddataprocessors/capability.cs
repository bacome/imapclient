using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCapabilityDataProcessor : iUnsolicitedDataProcessor
            {
                private static readonly cBytes kCapabilitySpace = new cBytes("CAPABILITY ");

                private readonly Action<cCapabilities, cCapabilities, cTrace.cContext> mReceivedCapabilities;

                public cCapabilityDataProcessor(Action<cCapabilities, cCapabilities, cTrace.cContext> pReceivedCapabilities)
                {
                    mReceivedCapabilities = pReceivedCapabilities;
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCapabilityDataProcessor), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kCapabilitySpace)) return eProcessDataResult.notprocessed;

                    if (!pCursor.ProcessCapability(out var lCapabilities, out var lAuthenticationMechanisms, lContext) && pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed capability response");
                        return eProcessDataResult.notprocessed;
                    }

                    lContext.TraceVerbose("got capabilities: {0} {1}", lCapabilities, lAuthenticationMechanisms);
                    mReceivedCapabilities(lCapabilities, lAuthenticationMechanisms, lContext);
                    return eProcessDataResult.processed;
                }
            }
        }
    }
}