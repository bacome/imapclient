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
            private static readonly cCommandPart kCloseCommandPart = new cCommandPart("CLOSE");

            public async Task CloseAsync(cMethodControl pMC, cMailboxId pMailboxId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CloseAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kCloseCommandPart);

                    ;?; // should be via mailboxcache
                    var lHook = new cCloseCommandHook(ZSetSelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("close success");
                        return;
                    }

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cCloseCommandHook : cCommandHook
            {
                private readonly Action<cSelectedMailbox, cTrace.cContext> mSetSelectedMailbox;

                public cCloseCommandHook(Action<cSelectedMailbox, cTrace.cContext> pSetSelectedMailbox)
                {
                    mSetSelectedMailbox = pSetSelectedMailbox ?? throw new ArgumentNullException(nameof(pSetSelectedMailbox));
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCloseCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) mSetSelectedMailbox(null, lContext);
                }
            }
        }
    }
}