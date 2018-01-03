using System;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kSetUnseenCountCommandPart = new cTextCommandPart("SEARCH UNSEEN");

            public async Task<cMessageHandleList> SetUnseenCountAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetUnseenCountAsync), pMC, pMailboxHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, null);

                    lBuilder.Add(await mSearchExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // search commands must be single threaded (so we can tell which result is which)

                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSetUnseenCountCommandPart);

                    var lHook = new cSetUnseenCountCommandHook(lSelectedMailbox);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("setunseencount success");
                        if (lHook.MessageHandles == null) throw new cUnexpectedServerActionException(lResult, "results not received on a successful setunseencount", 0, lContext);
                        return lHook.MessageHandles;
                    }

                    if (lHook.MessageHandles != null) lContext.TraceError("results received on a failed setunseencount");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cSetUnseenCountCommandHook : cCommandHookBaseSearch
            {
                private int mMessageCount;

                public cSetUnseenCountCommandHook(cSelectedMailbox pSelectedMailbox) : base(pSelectedMailbox) { }

                public cMessageHandleList MessageHandles { get; private set; } = null;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    mMessageCount = mSelectedMailbox.MessageCache.Count;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSetUnseenCountCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eCommandResultType.ok && mMSNs != null) MessageHandles = mSelectedMailbox.SetUnseenCount(mMessageCount, new cUIntList(mMSNs.Distinct()), lContext);
                }
            }
        }
    }
}