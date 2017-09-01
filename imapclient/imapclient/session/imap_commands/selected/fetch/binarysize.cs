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
            public async Task FetchBinarySizeAsync(cMethodControl pMC, iMessageHandle pHandle, string pPart, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchBinarySizeAsync), pMC, pHandle, pPart);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pPart == null) throw new ArgumentNullException(nameof(pPart));

                if (!cCommandPartFactory.TryAsAtom(pPart, out var lPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pHandle);

                    lBuilder.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // resolve the MSN
                    uint lMSN = lSelectedMailbox.GetMSN(pHandle);
                    if (lMSN == 0) throw new InvalidOperationException(); // likely expunged

                    // build command
                    lBuilder.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSN), kFetchCommandPartSpaceBinarySizeLBracket, lPart, cCommandPart.RBracket);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("fetch binary.size success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fKnownCapabilities.binary, lContext);
                    throw new cProtocolErrorException(lResult, fKnownCapabilities.binary, lContext);
                }
            }

            public async Task UIDFetchBinarySizeAsync(cMethodControl pMC, iMailboxHandle pHandle, cUID pUID, string pPart, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchBinarySizeAsync), pMC, pHandle, pUID, pPart);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pUID == null) throw new ArgumentNullException(nameof(pUID));
                if (pPart == null) throw new ArgumentNullException(nameof(pPart));

                if (!cCommandPartFactory.TryAsAtom(pPart, out var lPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    mMailboxCache.CheckIsSelectedMailbox(pHandle, pUID.UIDValidity);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    // set uidvalidity
                    lBuilder.AddUIDValidity(pUID.UIDValidity);

                    // build command
                    lBuilder.Add(kFetchCommandPartUIDFetchSpace, new cCommandPart(pUID.UID), kFetchCommandPartSpaceBinarySizeLBracket, lPart, cCommandPart.RBracket);

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid fetch binary.size success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fKnownCapabilities.binary, lContext);
                    throw new cProtocolErrorException(lResult, fKnownCapabilities.binary, lContext);
                }
            }
        }
    }
}