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
            private async Task<cBody> ZFetchBodyAsync(cMethodControl pMC, iMessageHandle pHandle, bool pBinary, cSection pSection, uint pOrigin, uint pLength, cTrace.cContext pParentContext)
            {
                // the caller must have checked that the binary option is compatible with the section (e.g. if binary is true the section can't specify a textpart)
                //  the length must be greater than zero

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchBodyAsync), pMC, pHandle, pBinary, pSection, pOrigin, pLength);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pSection == null) throw new ArgumentNullException(nameof(pSection));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pHandle);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be set before the handle is resolved
                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity);

                    // resolve the MSN
                    uint lMSN = lSelectedMailbox.GetMSN(pHandle);

                    if (lMSN == 0)
                    {
                        if (pHandle.Expunged) throw new cMessageExpungedException(pHandle);
                        throw new ArgumentOutOfRangeException(nameof(pHandle));
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

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("fetch body success");
                        if (lHook.Body == null) throw new cUnexpectedServerActionException(0, "body not received", lContext);
                        return lHook.Body;
                    }

                    if (lHook.Body != null) lContext.TraceError("received body on a failed fetch body");

                    fCapabilities lTryIgnoring;
                    if (pBinary) lTryIgnoring = fCapabilities.binary;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}