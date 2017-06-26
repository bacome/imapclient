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
            private async Task<cBody> ZFetchAsync(cMethodControl pMC, cMailboxId pMailboxId, iMessageHandle pHandle, cCapability pCapability, bool pBinary, cSection pSection, uint pOrigin, uint pLength, cTrace.cContext pParentContext)
            {
                // the caller must have checked that the binary option is compatible with the section (e.g. if binary is true the section can't specify a textpart)
                //  the length must be greater than zero

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchAsync), pMC, pMailboxId, pHandle, pCapability, pBinary, pSection, pOrigin, pLength);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);
                    lCommand.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lCommand.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be set before the handle is resolved
                    lCommand.UIDValidity = _SelectedMailbox.UIDValidity;

                    // resolve the MSN
                    uint lMSN = _SelectedMailbox.GetMSN(pHandle);
                    if (lMSN == 0) throw new cInvalidMessageHandleException(lContext); // either expunged or the cache has changed

                    // build command

                    lCommand.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSN));

                    if (pBinary) lCommand.Add(kFetchCommandPartSpaceBinaryPeekLBracket);
                    else lCommand.Add(kFetchCommandPartSpaceBodyPeekLBracket);

                    lCommand.Add(pSection, pOrigin, pLength);

                    // hook
                    var lHook = new cCommandHookFetchMSN(pCapability, pBinary, pSection, pOrigin, lMSN);
                    lCommand.Add(lHook);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("fetch section success");
                        if (lHook.Body == null) throw new cUnexpectedServerActionException(0, "body not received", lContext);
                        return lHook.Body;
                    }

                    if (lHook.Body != null) lContext.TraceError("received body on a failed fetch section");

                    fCapabilities lTryIgnoring;
                    if (pBinary) lTryIgnoring = fCapabilities.Binary;
                    else lTryIgnoring = 0;

                    if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}