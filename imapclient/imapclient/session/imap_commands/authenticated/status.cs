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
            private static readonly cCommandPart kStatusCommandPart = new cCommandPart("STATUS");

            public async Task<cMailboxStatus> StatusAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StatusAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                mMailboxCache.CheckHandle(pHandle);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lHandle = mMailboxCache.SelectedMailboxDetails?.Handle;
                    if (ReferenceEquals(pHandle, lHandle)) return lHandle.MailboxStatus;

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // status is msnunsafe

                    lCommand.BeginList(eListBracketing.none);
                    lCommand.Add(kStatusCommandPart);
                    lCommand.Add(pHandle.MailboxNameCommandPart);
                    lCommand.AddStatusAttributes(_Capability);
                    lCommand.EndList();

                    var lHook = new cStatusCommandHook(pHandle, mMailboxCache.Sequence);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("status success");
                        if (lHook.MailboxStatus == null) throw new cUnexpectedServerActionException(0, "result not received on a successful status", lContext); ?;
                        return lHook.MailboxStatus;
                    }

                    if (lHook.MailboxStatus != null) lContext.TraceError("result received on a failed status");

                    fCapabilities lTryIgnoring;
                    if (_Capability.CondStore) lTryIgnoring = fCapabilities.CondStore;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }

            private class cStatusCommandHook : cCommandHook
            {
                private readonly iMailboxHandle mHandle;
                private readonly int mSequence;

                public cStatusCommandHook(iMailboxHandle pHandle, int pSequence)
                {
                    mHandle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
                    mSequence = pSequence;
                }

                public cMailboxStatus MailboxStatus { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusCommandHook), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok && mHandle.Exists == true && mHandle.Status != null && mHandle.Status.Sequence >= mSequence) MailboxStatus = mHandle.MailboxStatus;
                }
            }
        }
    }
}