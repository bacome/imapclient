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
                if (_State != eState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle);

                    lCommand.Add(kSearchExtendedCommandPart);
                    lCommand.Add(pFilter, false, mEncodingPartFactory); // if the filter has UIDs in it, this makes the command sensitive to UIDValidity changes

                    var lHook = new cCommandHookSearchExtended(lCommand.Tag, lSelectedMailbox, false);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended search success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.ESearch, "results not received on a successful extended search", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended search");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.ESearch, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.ESearch, lContext);
                }
            }
        }
    }
}