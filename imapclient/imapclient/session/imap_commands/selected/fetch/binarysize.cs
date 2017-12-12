using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task FetchBinarySizeAsync(cMethodControl pMC, iMessageHandle pMessageHandle, string pPart, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchBinarySizeAsync), pMC, pMessageHandle, pPart);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
                if (pPart == null) throw new ArgumentNullException(nameof(pPart));

                if (!cCommandPartFactory.TryAsAtom(pPart, out var lPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pMessageHandle);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // uidvalidity must be captured before the handles are resolved
                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity);

                    // resolve the MSN
                    uint lMSN = lSelectedMailbox.GetMSN(pMessageHandle);

                    if (lMSN == 0)
                    {
                        if (pMessageHandle.Expunged) throw new cMessageExpungedException(pMessageHandle);
                        else throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
                    }

                    // build command
                    lBuilder.Add(kFetchCommandPartFetchSpace, new cTextCommandPart(lMSN), kFetchCommandPartSpaceBinarySizeLBracket, lPart, cCommandPart.RBracket);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("fetch binary.size success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.binary, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.binary, lContext);
                }
            }

            public async Task UIDFetchBinarySizeAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cUID pUID, string pPart, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchBinarySizeAsync), pMC, pMailboxHandle, pUID, pPart);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pUID == null) throw new ArgumentNullException(nameof(pUID));
                if (pPart == null) throw new ArgumentNullException(nameof(pPart));

                if (!cCommandPartFactory.TryAsAtom(pPart, out var lPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pUID.UIDValidity);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    // set uidvalidity
                    lBuilder.AddUIDValidity(pUID.UIDValidity);

                    // build command
                    lBuilder.Add(kFetchCommandPartUIDFetchSpace, new cTextCommandPart(pUID.UID), kFetchCommandPartSpaceBinarySizeLBracket, lPart, cCommandPart.RBracket);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid fetch binary.size success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.binary, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.binary, lContext);
                }
            }
        }
    }
}