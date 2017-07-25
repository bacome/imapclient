﻿using System;
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
            private async Task<cBody> ZUIDFetchBodyAsync(cMethodControl pMC, iMailboxHandle pHandle, cUID pUID, bool pBinary, cSection pSection, uint pOrigin, uint pLength, cTrace.cContext pParentContext)
            {
                // the caller must have checked that the binary option is compatible with the section (e.g. if binary is true the section can't specify a textpart)
                //  the length must be greater than zero

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchBodyAsync), pMC, pHandle, pUID, pBinary, pSection, pOrigin, pLength);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pUID == null) throw new ArgumentNullException(nameof(pUID));
                if (pSection == null) throw new ArgumentNullException(nameof(pSection));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    mMailboxCache.CheckIsSelectedMailbox(pHandle);

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    // set uidvalidity
                    lCommand.AddUIDValidity(pUID.UIDValidity);

                    // build command

                    lCommand.Add(kFetchCommandPartUIDFetchSpace, new cCommandPart(pUID.UID));

                    if (pBinary) lCommand.Add(kFetchCommandPartSpaceBinaryPeekLBracket);
                    else lCommand.Add(kFetchCommandPartSpaceBodyPeekLBracket);

                    lCommand.Add(pSection, pOrigin, pLength);

                    // hook
                    var lHook = new cCommandHookFetchUID(pBinary, pSection, pOrigin, pUID.UID);
                    lCommand.Add(lHook);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid fetch section success");
                        if (lHook.Body == null) throw new cUnexpectedServerActionException(0, "body not received", lContext);
                        return lHook.Body;
                    }

                    if (lHook.Body != null) lContext.TraceError("received body on a failed uid fetch section");

                    fCapabilities lTryIgnoring;
                    if (pBinary) lTryIgnoring = fCapabilities.Binary;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}