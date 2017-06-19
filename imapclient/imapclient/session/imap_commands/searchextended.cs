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
            private static readonly cCommandPart kSearchExtendedCommandPart = new cCommandPart("SEARCH RETURN () ");

            public async Task<cHandleList> SearchExtendedAsync(cMethodControl pMC, cMailboxId pMailboxId, cFilter pFilter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SearchExtendedAsync), pMC, pMailboxId, pFilter);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);

                    lCommand.Add(kSearchExtendedCommandPart);
                    lCommand.Add(pFilter, false, EnabledExtensions, mEncoding); // if the filter has UIDs in it, this makes the command sensitive to UIDValidity changes

                    var lHook = new cCommandHookSearchExtended(lCommand.Tag, _SelectedMailbox, false);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.ESearch, "results not received on a successful extended search", lContext);
                        lContext.TraceInformation("extended search success");
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended search");

                    if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.ESearch, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.ESearch, lContext);
                }
            }
        }
    }
}