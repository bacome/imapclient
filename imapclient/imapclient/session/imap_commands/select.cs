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
            private static readonly cCommandPart kSelectCommandPart = new cCommandPart("SELECT ");
            private static readonly cCommandPart kSelectCommandPartCondStore = new cCommandPart(" (CONDSTORE)");

            private async Task ZSelectAsync(cMethodControl pMC, cMailboxId pMailboxId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SelectAsync), pMC, pMailboxId);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                if (!mStringFactory.TryAsMailbox(pMailboxId.MailboxName, out var lMailboxCommandPart, out _)) throw new ArgumentOutOfRangeException(nameof(pMailboxId));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false));

                    lCommand.Add(kSelectCommandPart, lMailboxCommandPart);
                    if (_Capability.CondStore) lCommand.Add(kExamineCommandPartCondStore);

                    var lHook = new cCommandHookSelect(_SelectedMailbox != null, _Capability, new cSelectedMailbox(pMailboxId, true, mEventSynchroniser, ZGetCapability), ZSetSelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("select success");
                        return;
                    }

                    fCapabilities lTryIgnoring;
                    if (_Capability.CondStore) lTryIgnoring = fCapabilities.CondStore;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}