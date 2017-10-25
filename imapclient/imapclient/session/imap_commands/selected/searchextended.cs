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
            private static readonly cCommandPart kSearchExtendedCommandPart = new cTextCommandPart("SEARCH RETURN () ");

            public async Task<cMessageHandleList> SearchExtendedAsync(cMethodControl pMC, iMailboxHandle pHandle, cFilter pFilter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SearchExtendedAsync), pMC, pHandle, pFilter);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, pFilter.UIDValidity);

                    // special cases
                    if (ReferenceEquals(pFilter, cFilter.All)) return new cMessageHandleList(lSelectedMailbox.Cache);
                    if (ReferenceEquals(pFilter, cFilter.False)) return new cMessageHandleList();

                    if (pFilter.ContainsMessageHandles) lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSearchExtendedCommandPart);
                    lBuilder.Add(pFilter, lSelectedMailbox, false, mEncodingPartFactory);

                    var lHook = new cCommandHookSearchExtended(lBuilder.Tag, lSelectedMailbox, false);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended search success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.esearch, "results not received on a successful extended search", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended search");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.esearch, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.esearch, lContext);
                }
            }
        }
    }
}