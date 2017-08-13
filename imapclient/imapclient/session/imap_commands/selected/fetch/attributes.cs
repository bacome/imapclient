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
            private async Task ZFetchAttributesAsync(cMethodControl pMC, cMessageHandleList pHandles, fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchAttributesAsync), pMC, pHandles, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
                if (pHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pHandles));
                if (pAttributes == 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pHandles);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // resolve MSNs

                    cUIntList lMSNs = new cUIntList();

                    foreach (var lHandle in pHandles)
                    {
                        var lMSN = lSelectedMailbox.GetMSN(lHandle);
                        if (lMSN != 0) lMSNs.Add(lMSN);
                    }

                    if (lMSNs.Count == 0) return;

                    // build command

                    lBuilder.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSNs.ToSequenceSet()), cCommandPart.Space);
                    lBuilder.Add(pAttributes);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("fetch success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}