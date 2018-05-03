﻿using System;
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
            private async Task<cBody> ZUIDFetchBodyAsync(iMailboxHandle pMailboxHandle, cUID pUID, bool pBinary, cSection pSection, uint pOrigin, uint pLength, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
            {
                // the caller must have checked that the binary option is compatible with the section (e.g. if binary is true the section can't specify a textpart)
                //  the length must be greater than zero

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchBodyAsync), pMailboxHandle, pUID, pBinary, pSection, pOrigin, pLength);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pUID == null) throw new ArgumentNullException(nameof(pUID));
                if (pSection == null) throw new ArgumentNullException(nameof(pSection));

                var lMC = new cMethodControl(pCancellationToken);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(lMC, lContext).ConfigureAwait(false)); // block select

                    mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pUID.UIDValidity);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(lMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

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

                    var lResult = await mPipeline.ExecuteAsync(lMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid fetch body success");
                        if (lHook.Body == null) throw new cRequestedIMAPDataNotReturnedException(pMailboxHandle, pUID);
                        return lHook.Body;
                    }

                    if (lHook.Body != null) lContext.TraceError("received body on a failed uid fetch body");

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