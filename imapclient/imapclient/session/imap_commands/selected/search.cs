using System;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kSearchCommandPart = new cTextCommandPart("SEARCH ");

            public async Task<cMessageHandleList> SearchAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SearchAsync), pMC, pMailboxHandle, pFilter);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pFilter.UIDValidity);

                    // special cases
                    if (ReferenceEquals(pFilter, cFilter.All)) return new cMessageHandleList(lSelectedMailbox.MessageCache);
                    if (ReferenceEquals(pFilter, cFilter.None)) return new cMessageHandleList();

                    lBuilder.Add(await mSearchExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // search commands must be single threaded (so we can tell which result is which)
                    if (pFilter.ContainsMessageHandles) lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSearchCommandPart);
                    lBuilder.Add(pFilter, lSelectedMailbox, false, mEncodingPartFactory);

                    var lHook = new cSearchCommandHook(lSelectedMailbox);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("search success");
                        if (lHook.MessageHandles == null) throw new cUnexpectedServerActionException(lResult, "results not received on a successful search", 0, lContext);
                        return lHook.MessageHandles;
                    }

                    if (lHook.MessageHandles != null) lContext.TraceError("results received on a failed search");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cSearchCommandHook : cCommandHookBaseSearch
            {
                public cSearchCommandHook(cSelectedMailbox pSelectedMailbox) : base(pSelectedMailbox) { }

                public cMessageHandleList MessageHandles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSearchCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eCommandResultType.ok || mMSNs == null) return;
                    MessageHandles = new cMessageHandleList(mMSNs.Distinct().Select(lMSN => mSelectedMailbox.GetHandle(lMSN)));
                }
            }
        }
    }
}