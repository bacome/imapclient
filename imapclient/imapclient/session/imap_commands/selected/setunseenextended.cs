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
            private static readonly cCommandPart kSetUnseenExtendedCommandPart = new cCommandPart("SEARCH RETURN () UNSEEN");

            public async Task<cMessageHandleList> SetUnseenExtendedAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetUnseenExtendedAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle);

                    lCommand.Add(kSetUnseenExtendedCommandPart);

                    var lHook = new cSetUnseenExtendedCommandHook(lCommand.Tag, lSelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended setunseen success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.ESearch, "results not received on a successful extended setunseen", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended setunseen");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.ESearch, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.ESearch, lContext);
                }
            }

            private class cSetUnseenExtendedCommandHook : cCommandHookBaseSearchExtended
            {
                public cSetUnseenExtendedCommandHook(cCommandTag pCommandTag, cSelectedMailbox pSelectedMailbox) : base(pCommandTag, pSelectedMailbox) { }

                public cMessageHandleList Handles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSetUnseenExtendedCommandHook), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok && mSequenceSets != null)
                    {
                        var lMSNs = cUIntList.FromSequenceSets(mSequenceSets, (uint)mSelectedMailbox.Cache.MessageCount);
                        lMSNs = lMSNs.ToSortedUniqueList();
                        Handles = mSelectedMailbox.SetUnseen(lMSNs, lContext);
                    }
                }
            }
        }
    }
}