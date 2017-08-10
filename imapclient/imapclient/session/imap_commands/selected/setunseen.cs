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
            private static readonly cCommandPart kSetUnseenCommandPart = new cCommandPart("SEARCH UNSEEN");

            public async Task<cMessageHandleList> SetUnseenAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetUnseenAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle);

                    lCommand.Add(await mSearchExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // search commands must be single threaded (so we can tell which result is which)

                    lCommand.Add(kSetUnseenCommandPart);

                    var lHook = new cSetUnseenCommandHook(lSelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("setunseen success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(0, "results not received on a successful setunseen", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed setunseen");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cSetUnseenCommandHook : cCommandHookBaseSearch
            {
                public cSetUnseenCommandHook(cSelectedMailbox pSelectedMailbox) : base(pSelectedMailbox) { }

                public cMessageHandleList Handles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSetUnseenCommandHook), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok && mMSNs != null) Handles = mSelectedMailbox.SetUnseen(mMSNs, lContext);
                }
            }
        }
    }
}