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
            private async Task<cBody> ZUIDFetchBodyAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cUID pUID, bool pBinary, cSection pSection, uint pOrigin, uint pLength, cTrace.cContext pParentContext)
            {
                // the caller must have checked that the binary option is compatible with the section (e.g. if binary is true the section can't specify a textpart)
                //  the length must be greater than zero

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchBodyAsync), pMC, pMailboxHandle, pUID, pBinary, pSection, pOrigin, pLength);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pUID == null) throw new ArgumentNullException(nameof(pUID));
                if (pSection == null) throw new ArgumentNullException(nameof(pSection));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pUID.UIDValidity);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    // set uidvalidity
                    lBuilder.AddUIDValidity(pUID.UIDValidity);

                    // build command

                    lBuilder.Add(kFetchCommandPartUIDFetchSpace, new cTextCommandPart(pUID.UID));

                    if (pBinary) lBuilder.Add(kFetchCommandPartSpaceBinaryPeekLBracket);
                    else lBuilder.Add(kFetchCommandPartSpaceBodyPeekLBracket);

                    lBuilder.Add(pSection, pOrigin, pLength);

                    // hook
                    var lHook = new cCommandHookFetchBodyUID(pBinary, pSection, pOrigin, pUID.UID);
                    lBuilder.Add(lHook);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid fetch body success");
                        if (lHook.Body == null) throw new cRequestedDataNotReturnedException(pMailboxHandle, pUID);
                        return lHook.Body;
                    }

                    if (lHook.Body != null) lContext.TraceError("received body on a failed uid fetch body");

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