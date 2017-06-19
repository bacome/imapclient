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
            private static readonly cCommandPart kSelectCommandPartSelect = new cCommandPart("SELECT ");
            private static readonly cCommandPart kSelectCommandPartExamine = new cCommandPart("EXAMINE ");

            public async Task SelectAsync(cMethodControl pMC, cMailboxId pMailboxId, bool pForUpdate, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SelectAsync), pMC, pMailboxId, pForUpdate);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                cCommandPart.cFactory lFactory = new cCommandPart.cFactory((EnabledExtensions & fEnableableExtensions.utf8) != 0);
                if (!lFactory.TryAsMailbox(pMailboxId.MailboxName, out var lMailboxCommandPart, out _)) throw new ArgumentOutOfRangeException(nameof(pMailboxId));

                Task<cCommandResult> lTask;

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false));

                    if (pForUpdate) lCommand.Add(kSelectCommandPartSelect, lMailboxCommandPart);
                    else lCommand.Add(kSelectCommandPartExamine, lMailboxCommandPart);

                    lCommand.Add(new cSelectCommandHook(new cSelectedMailbox(pMailboxId, pForUpdate, mEventSynchroniser, _Capability), ZSetSelectedMailbox));

                    try
                    {
                        // note that if either of the following two calls throw we c/would be in an inconsistent state with the server
                        ZSetSelectedMailbox(null, lContext); // set the selected mailbox to null
                        lTask = mPipeline.ExecuteAsync(pMC, lCommand, lContext);
                    }
                    catch
                    {
                        Disconnect(lContext);
                        throw;
                    }
                }

                var lResult = await lTask.ConfigureAwait(false);

                if (lResult.Result == cCommandResult.eResult.ok)
                {
                    lContext.TraceInformation("select command success");
                    return;
                }

                if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                throw new cProtocolErrorException(lResult, 0, lContext);
            }

            private class cSelectCommandHook : cCommandHook
            {
                private readonly cSelectedMailbox mSelectedMailbox;
                private readonly Action<cSelectedMailbox, cTrace.cContext> mSetSelectedMailbox;

                public cSelectCommandHook(cSelectedMailbox pSelectedMailbox, Action<cSelectedMailbox, cTrace.cContext> pSetSelectedMailbox)
                {
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                    mSetSelectedMailbox = pSetSelectedMailbox ?? throw new ArgumentNullException(nameof(pSetSelectedMailbox));
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => mSelectedMailbox.ProcessData(pCursor, pParentContext);
                public override bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext) => mSelectedMailbox.ProcessTextCode(pCursor, pParentContext);

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSelectCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult != null && pResult.Result == cCommandResult.eResult.ok) mSetSelectedMailbox(mSelectedMailbox, lContext);
                }
            }
        }
    }
}