using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private async Task<cBody> ZFetchBodyAsync(iMessageHandle pMessageHandle, bool pBinary, cSection pSection, uint pOrigin, uint pLength, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
            {
                // the caller must have checked that the binary option is compatible with the section (e.g. if binary is true the section can't specify a textpart)
                //  the length must be greater than zero

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchBodyAsync), pMessageHandle, pBinary, pSection, pOrigin, pLength);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
                if (pSection == null) throw new ArgumentNullException(nameof(pSection));

                var lMC = new cMethodControl(pCancellationToken);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(lMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pMessageHandle);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(lMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(lMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be set before the handle is resolved
                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity);

                    // resolve the MSN
                    uint lMSN = lSelectedMailbox.GetMSN(pMessageHandle);

                    if (lMSN == 0)
                    {
                        if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                        throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
                    }

                    // build command

                    lBuilder.Add(kFetchCommandPartFetchSpace, new cTextCommandPart(lMSN));

                    if (pBinary) lBuilder.Add(kFetchCommandPartSpaceBinaryPeekLBracket);
                    else lBuilder.Add(kFetchCommandPartSpaceBodyPeekLBracket);

                    lBuilder.Add(pSection, pOrigin, pLength);

                    // hook
                    var lHook = new cCommandHookFetchBodyMSN(pBinary, pSection, pOrigin, lMSN);
                    lBuilder.Add(lHook);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(lMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("fetch body success");
                        if (lHook.Body == null) throw new cRequestedIMAPDataNotReturnedException(pMessageHandle);
                        return lHook.Body;
                    }

                    if (lHook.Body != null) lContext.TraceError("received body on a failed fetch body");

                    fIMAPCapabilities lTryIgnoring;
                    if (pBinary) lTryIgnoring = fIMAPCapabilities.binary;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cIMAPProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}