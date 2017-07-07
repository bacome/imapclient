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
            private static readonly cCommandPart kExamineCommandPart = new cCommandPart("EXAMINE ");
            private static readonly cCommandPart kExamineCommandPartCondStore = new cCommandPart(" (CONDSTORE)");

            private async Task ZExamineAsync(cMethodControl pMC, cMailboxId pMailboxId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ExamineAsync), pMC, pMailboxId);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                cCommandPart.cFactory lFactory = new cCommandPart.cFactory((EnabledExtensions & fEnableableExtensions.utf8) != 0);
                if (!lFactory.TryAsMailbox(pMailboxId.MailboxName, out var lMailboxCommandPart, out _)) throw new ArgumentOutOfRangeException(nameof(pMailboxId));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false));

                    lCommand.Add(kExamineCommandPart, lMailboxCommandPart);
                    if (_Capability.CondStore) lCommand.Add(kExamineCommandPartCondStore);

                    var lHook = new cCommandHookSelect(_SelectedMailbox != null, _Capability, new cSelectedMailbox(pMailboxId, false, mEventSynchroniser, ZGetCapability), ZSetSelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("examine success");
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