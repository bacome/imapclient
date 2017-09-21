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
            private static readonly cCommandPart kSortExtendedCommandPart = new cCommandPart("SORT RETURN () ");

            public async Task<cMessageHandleList> SortExtendedAsync(cMethodControl pMC, iMailboxHandle pHandle, cFilter pFilter, cSort pSort, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SortExtendedAsync), pMC, pHandle, pFilter, pSort);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
                if (pSort == null) throw new ArgumentNullException(nameof(pSort));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, pFilter.UIDValidity);

                    // special case
                    if (ReferenceEquals(pFilter, cFilter.False)) return new cMessageHandleList();

                    if (pFilter.ContainsMessageHandles) lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    lBuilder.AddUIDValidity(lSelectedMailbox.Cache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSortExtendedCommandPart);
                    lBuilder.Add(pSort);
                    lBuilder.Add(cCommandPart.Space);
                    lBuilder.Add(pFilter, lSelectedMailbox, true, mEncodingPartFactory);

                    var lHook = new cCommandHookSearchExtended(lBuilder.Tag, lSelectedMailbox, true);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended sort success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.esort, "results not received on a successful extended sort", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed extended sort");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.esort, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.esort, lContext);
                }
            }
        }
    }
}