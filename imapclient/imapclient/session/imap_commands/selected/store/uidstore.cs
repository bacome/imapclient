using System;
using System.Collections.Generic;
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
            private static readonly cCommandPart kUIDStoreCommandPartUIDStoreSpace = new cCommandPart("UID STORE ");

            private async Task ZUIDStoreAsync(cMethodControl pMC, iMailboxHandle pHandle, uint pUIDValidity, cStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZStoreAsync), pMC, pHandle, pUIDValidity, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                // no validation ... all the parameters have been validated already by the cSession by the time we get here

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, pUIDValidity);
                    if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException();

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.AddUIDValidity(pUIDValidity); // the command is sensitive to UIDValidity changes

                    // build the command
                    lBuilder.Add(kUIDStoreCommandPartUIDStoreSpace, new cCommandPart(cSequenceSet.FromUInts(pFeedback.UInts)), cCommandPart.Space);
                    if (pIfUnchangedSinceModSeq != null) lBuilder.Add(kStoreCommandPartLParenUnchangedSinceSpace, new cCommandPart(pIfUnchangedSinceModSeq.Value), kStoreCommandPartRParenSpace);
                    lBuilder.Add(pOperation, pFlags);

                    // add the hook
                    var lHook = new cCommandHookStore(pFeedback, lSelectedMailbox);
                    lBuilder.Add(lHook);

                    // submit the command                
                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);
                    // note: the base spec states that non-existent UIDs are ignored without comment => a NO from a UID STORE is unexpected

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid store success");
                        return;
                    }

                    fCapabilities lTryIgnoring;
                    if (pIfUnchangedSinceModSeq == null) lTryIgnoring = 0;
                    else lTryIgnoring = fCapabilities.condstore;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}

