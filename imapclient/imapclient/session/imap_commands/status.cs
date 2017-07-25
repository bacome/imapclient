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
            private static readonly cCommandPart kStatusCommandPart = new cCommandPart("STATUS ");

            public async Task<cMailboxStatus> StatusAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StatusAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();

                mMailboxCache.CheckHandle(pHandle, lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lHandle = mMailboxCache.SelectedMailboxDetails?.Handle;
                    if (lHandle == pHandle) return lHandle.MailboxStatus;

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // status is msnunsafe


                    ccom


                    lCommand.Add(kStatusStatusCommandPart);
                    lCommand.Add(lMailboxCommandPart);
                    lCommand.Add(kStatusCommandPartrfc3501Attributes);
                    if (_Capability.CondStore) lCommand.Add(kStatusCommandPartHighestModSeq);
                    lCommand.Add(cCommandPart.RParen);

                    var lHook = new cStatusCommandHook(lEncodedMailboxName);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("status success");

                        var lStatus = lHook.Status;

                        if (lStatus == null || lStatus.Messages == null || lStatus.Recent == null || lStatus.Unseen == null) throw new cUnexpectedServerActionException(0, "status not received", lContext);

                        cMailboxStatus lMailboxStatus = new cMailboxStatus(lStatus.Messages.Value, lStatus.Recent.Value, lStatus.UIDNext ?? 0, 0, lStatus.UIDValidity ?? 0, lStatus.Unseen.Value, 0, lStatus.HighestModSeq ?? 0);

                        mMailboxCache.SetStatus(pMailboxId.MailboxName, lMailboxStatus);

                        return lMailboxStatus;
                    }

                    if (lHook.Status != null) lContext.TraceError("received status on a failed status");

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