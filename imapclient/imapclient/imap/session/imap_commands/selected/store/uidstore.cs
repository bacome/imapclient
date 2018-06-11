using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kUIDStoreCommandPartUIDStoreSpace = new cTextCommandPart("UID STORE ");

            private async Task ZUIDStoreAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, uint pUIDValidity, cStoreFeedbackCollector pFeedbackCollector, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cUIDStoreFeedback pFeedback, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDStoreAsync), pMC, pMailboxHandle, pUIDValidity, pFeedbackCollector, pOperation, pFlags, pIfUnchangedSinceModSeq, pFeedback);

                // no validation ... all the parameters have been validated already by the cSession by the time we get here

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pUIDValidity);
                    if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.AddUIDValidity(pUIDValidity); // the command is sensitive to UIDValidity changes

                    // build the command
                    lBuilder.Add(kUIDStoreCommandPartUIDStoreSpace, new cTextCommandPart(cSequenceSet.FromUInts(pFeedbackCollector.UInts)), cCommandPart.Space);
                    if (pIfUnchangedSinceModSeq != null) lBuilder.Add(kStoreCommandPartLParenUnchangedSinceSpace, new cTextCommandPart(pIfUnchangedSinceModSeq.Value), kStoreCommandPartRParenSpace);
                    lBuilder.Add(pOperation, pFlags);

                    // add the hook
                    var lHook = new cCommandHookStore(pFeedbackCollector, pFeedback, lSelectedMailbox);
                    lBuilder.Add(lHook);

                    // submit the command                
                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);
                    // note: the base spec states that non-existent UIDs are ignored without comment => a NO from a UID STORE is unexpected

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid store success");
                        return;
                    }

                    fIMAPCapabilities lTryIgnoring;
                    if (pIfUnchangedSinceModSeq == null) lTryIgnoring = 0;
                    else lTryIgnoring = fIMAPCapabilities.condstore;

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cIMAPProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}

