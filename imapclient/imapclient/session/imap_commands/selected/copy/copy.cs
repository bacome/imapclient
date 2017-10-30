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
            private static readonly cCommandPart kCopyCommandPart = new cTextCommandPart("COPY ");

            private async Task<cCopyFeedback> ZCopyAsync(cMethodControl pMC, cMessageHandleList pSourceHandles, cMailboxCacheItem pDestinationItem, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZCopyAsync), pMC, pSourceHandles, pDestinationItem);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pSourceHandles);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be captured before the handles are resolved
                    var lUIDValidity = lSelectedMailbox.Cache.UIDValidity;
                    lBuilder.AddUIDValidity(lUIDValidity);

                    // resolve the handles to MSNs

                    cUIntList lMSNs = new cUIntList();

                    foreach (var lHandle in pSourceHandles)
                    {
                        var lMSN = lSelectedMailbox.GetMSN(lHandle);
                        if (lMSN == 0) throw new InvalidOperationException();
                        lMSNs.Add(lMSN);
                    }

                    // build the command
                    lBuilder.Add(kCopyCommandPart, new cTextCommandPart(cSequenceSet.FromUInts(lMSNs)), cCommandPart.Space, pDestinationItem.MailboxNameCommandPart);

                    // add the hook
                    var lHook = new cCommandHookCopy(lUIDValidity);
                    lBuilder.Add(lHook);

                    // submit the command                
                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("copy success");
                        return lHook.Feedback;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}