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
            private static readonly cCommandPart kSearchExtendedCommandPart = new cCommandPart("SEARCH RETURN () ");

            public async Task<cMessageHandleList> SearchExtendedAsync(cMethodControl pMC, iMailboxHandle pHandle, cFilter pFilter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SearchExtendedAsync), pMC, pHandle, pFilter);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle);

                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity);

                    lBuilder.Add(kSearchExtendedCommandPart);
                    lBuilder.Add(pFilter, false, mEncodingPartFactory);

                    var lHook = new cCommandHookSearchExtended(lBuilder.Tag, lSelectedMailbox, false);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended search success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fKnownCapabilities.esearch, "results not received on a successful extended search", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended search");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fKnownCapabilities.esearch, lContext);
                    throw new cProtocolErrorException(lResult, fKnownCapabilities.esearch, lContext);
                }
            }
        }
    }
}