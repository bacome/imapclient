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
            private static readonly cCommandPart kSetUnseenCountExtendedCommandPart = new cTextCommandPart("SEARCH RETURN () UNSEEN");

            public async Task<cMessageHandleList> SetUnseenCountExtendedAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SetUnseenCountExtendedAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, null);

                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSetUnseenCountExtendedCommandPart);

                    var lHook = new cSetUnseenCountExtendedCommandHook(lBuilder.Tag, lSelectedMailbox);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended setunseencount success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.esearch, "results not received on a successful extended setunseencount", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended setunseencount");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.esearch, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.esearch, lContext);
                }
            }

            private class cSetUnseenCountExtendedCommandHook : cCommandHookBaseSearchExtended
            {
                private int mMessageCount;

                public cSetUnseenCountExtendedCommandHook(cCommandTag pCommandTag, cSelectedMailbox pSelectedMailbox) : base(pCommandTag, pSelectedMailbox) { }

                public cMessageHandleList Handles { get; private set; } = null;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    mMessageCount = mSelectedMailbox.Cache.Count;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSetUnseenCountExtendedCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eCommandResultType.ok || mSequenceSets == null) return;
                    if (!cUIntList.TryConstruct(mSequenceSets, mSelectedMailbox.Cache.Count, true, out var lMSNs)) return;
                    Handles = mSelectedMailbox.SetUnseenCount(mMessageCount, lMSNs, lContext);
                }
            }
        }
    }
}