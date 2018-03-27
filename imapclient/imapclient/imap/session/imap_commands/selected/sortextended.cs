using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kSortExtendedCommandPart = new cTextCommandPart("SORT RETURN () ");

            public async Task<cMessageHandleList> SortExtendedAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SortExtendedAsync), pMC, pMailboxHandle, pFilter, pSort);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
                if (pSort == null) throw new ArgumentNullException(nameof(pSort));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pFilter.UIDValidity);

                    // special case
                    if (ReferenceEquals(pFilter, cFilter.None)) return new cMessageHandleList();

                    if (pFilter.ContainsMessageHandles) lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSortExtendedCommandPart);
                    lBuilder.Add(pSort);
                    lBuilder.Add(cCommandPart.Space);
                    lBuilder.Add(pFilter, lSelectedMailbox, true, mEncodingPartFactory);

                    var lHook = new cCommandHookSearchExtended(lBuilder.Tag, lSelectedMailbox, true);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("extended sort success");
                        if (lHook.MessageHandles == null) throw new cUnexpectedServerActionException(lResult, "results not received on a successful extended sort", fIMAPCapabilities.esort, lContext);
                        return lHook.MessageHandles;
                    }

                    if (lHook.MessageHandles != null) lContext.TraceError("results received on a failed extended sort");

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, fIMAPCapabilities.esort, lContext);
                    throw new cIMAPProtocolErrorException(lResult, fIMAPCapabilities.esort, lContext);
                }
            }
        }
    }
}