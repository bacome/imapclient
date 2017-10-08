﻿using System;
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

            public async Task ZStoreAsync(cMethodControl pMC, cStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZStoreAsync), pMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                // no validation ... all the parameters have been validated already by the cSession by the time we get here

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pFeedback);
                    if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException();

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be captured before the handles are resolved
                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity);

                    // working storage
                    cStoreFeedbacker lFeedbacker = new cStoreFeedbacker();

                    // resolve the handles to MSNs
                    foreach (var lItem in pFeedback)
                    {
                        var lMSN = lSelectedMailbox.GetMSN(lItem.Handle);
                        if (lMSN != 0) lFeedbacker.Add(lMSN, lItem);
                    }

                    // if no handles were resolved, we are done
                    if (lFeedbacker.Count == 0) return;

                    // build the command
                    lBuilder.Add(kStoreCommandPartStoreSpace, new cCommandPart(cSequenceSet.FromUInts(lFeedbacker.UInts)), cCommandPart.Space);
                    if (pIfUnchangedSinceModSeq != null) lBuilder.Add(kStoreCommandPartLParenUnchangedSinceSpace, new cCommandPart(pIfUnchangedSinceModSeq.Value), kStoreCommandPartRParenSpace);
                    lBuilder.Add(pOperation, pFlags);

                    // add the hook
                    var lHook = new cCommandHookStore(lFeedbacker, lSelectedMailbox);
                    lBuilder.Add(lHook);

                    // submit the command                
                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);
                    // NOTE: updates may have been done on both OK and NO responses (see rfc 2180/ 7162) so we can't throw on a NO for this command ... NO indicates that some (or all) of the messages have pending deletes

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
                }
            }
        }
    }
}

