using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private async Task ZFetchCacheItemsAsync(cMethodControl pMC, cMessageHandleList pMessageHandles, cMessageCacheItems pItems, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchCacheItemsAsync), pMC, pMessageHandles, pItems);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
                if (pItems == null) throw new ArgumentNullException(nameof(pItems));

                if (pMessageHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pMessageHandles));
                if (pItems.IsEmpty) throw new ArgumentOutOfRangeException(nameof(pItems));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pMessageHandles);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be captured before the handles are resolved
                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity);

                    // resolve MSNs

                    cUIntList lMSNs = new cUIntList();

                    foreach (var lMessageHandle in pMessageHandles)
                    {
                        var lMSN = lSelectedMailbox.GetMSN(lMessageHandle);
                        if (lMSN != 0) lMSNs.Add(lMSN);
                    }

                    if (lMSNs.Count == 0) return;

                    // build command

                    lBuilder.Add(kFetchCommandPartFetchSpace, new cTextCommandPart(cSequenceSet.FromUInts(lMSNs)), cCommandPart.Space);
                    lBuilder.Add(pItems, lSelectedMailbox.MessageCache.NoModSeq);

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