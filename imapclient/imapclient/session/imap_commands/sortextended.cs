using System;
using System.Collections.Generic;
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
            private static readonly cCommandPart kSortExtendedCommandPart = new cCommandPart("SORT RETURN () ");

            public async Task<cHandleList> SortExtendedAsync(cMethodControl pMC, cMailboxId pMailboxId, cFilter pFilter, cSort pSort, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SortExtendedAsync), pMC, pMailboxId, pFilter, pSort);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);

                    lCommand.Add(kSortExtendedCommandPart);
                    lCommand.Add(pSort);
                    lCommand.Add(cCommandPart.Space);
                    lCommand.Add(pFilter, true, EnabledExtensions, mEncoding); // if the filter has UIDs in it, this makes the command sensitive to UIDValidity changes

                    var lHook = new cCommandHookSearchExtended(lCommand.Tag, _SelectedMailbox, true);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("extended sort success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.ESort, "results not received on a successful extended sort", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended sort");

                    if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.ESort, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.ESort, lContext);
                }
            }
        }
    }
}