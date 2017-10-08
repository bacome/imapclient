using System;
using System.Linq;
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
            private static readonly cCommandPart kStoreCommandPartStoreSpace = new cCommandPart("STORE ");

            public async Task<bool> ZStoreAsync(cMethodControl pMC, cMessageHandleList pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZStoreAsync), pMC, pHandles, pOperation, pFlags, pIfUnchangedSinceModSeq);

                // no validation ... all the parameters have been validated already by the cSession by the time we get here

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pHandles);
                    if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException();

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be captured before the handles are resolved
                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity);

                    // working storage
                    cStoreFeedback lFeedback = new cStoreFeedback(false);
                    cMessageHandleList lFailedToStore = new cMessageHandleList();

                    // resolve the handles to MSNs
                    foreach (var lHandle in pHandles)
                    {
                        var lMSN = lSelectedMailbox.GetMSN(lHandle);
                        if (lMSN == 0) lFailedToStore.Add(lHandle);
                        else lFeedback.Add(lMSN, lHandle);
                    }

                    // if no handles were resolved, we are done
                    if (lFeedback.Count == 0) return lFailedToStore;

                    // build the command
                    lBuilder.Add(kStoreCommandPartStoreSpace, new cCommandPart(cSequenceSet.FromUInts(lFeedback.UInts)), cCommandPart.Space);
                    if (pIfUnchangedSinceModSeq != null) lBuilder.Add(kStoreCommandPartLParenUnchangedSinceSpace, new cCommandPart(pIfUnchangedSinceModSeq.Value), kStoreCommandPartRParenSpace);
                    lBuilder.Add(pOperation, pFlags);

                    // add the hook
                    var lHook = new cCommandHookStore(lFeedback, lSelectedMailbox);
                    lBuilder.Add(lHook);

                    // submit the command                
                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);
                    // NOTE: both OK and NO responses may indicate that updates have occurred (see rfc 2180/ 7162) so we can't throw on a NO for this command ... NO MAY be indicating that some (or all) of the messages have been deleted

                    // throw on a bad
                    if (lResult.ResultType == eCommandResultType.bad)
                    {
                        fCapabilities lTryIgnoring;
                        if (pIfUnchangedSinceModSeq == null) lTryIgnoring = 0;
                        else lTryIgnoring = fCapabilities.condstore;
                        throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                    }

                    if (lResult.ResultType == eCommandResultType.ok) lContext.TraceInformation("store success");
                    else lContext.TraceInformation("store possible partial success");

                    lFailedToStore.AddRange(from i in lFeedback.Items where !i.Fetched || i.Modified select i.Handle);

                    return lFailedToStore;
                }
            }
        }
    }
}

