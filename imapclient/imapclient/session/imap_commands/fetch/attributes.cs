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
            private async Task ZFetchAttributesAsync(cMethodControl pMC, cMailboxId pMailboxId, cHandleList pHandles, fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                // note that this silently fails if the handles are out of date
                //  AND if a UID validity change were to happen during the run it wouldn't complain either
                //
                // note that the caller should have checked that pHandles is non-null and contains no null entries and that pAttributes contains some attributes to fetch

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchAsync), pMC, pMailboxId, pHandles, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);
                    lCommand.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lCommand.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // resolve MSNs

                    cUIntList lMSNs = new cUIntList();

                    foreach (var lHandle in pHandles)
                    {
                        var lMSN = _SelectedMailbox.GetMSN(lHandle);
                        if (lMSN != 0) lMSNs.Add(lMSN);
                    }

                    if (lMSNs.Count == 0) return;

                    // build command

                    lCommand.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSNs.ToSequenceSet()), cCommandPart.Space);
                    lCommand.Add(pAttributes);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

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