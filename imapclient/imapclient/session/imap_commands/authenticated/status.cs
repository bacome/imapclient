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
            private static readonly cCommandPart kStatusCommandPart = new cCommandPart("STATUS");

            public async Task<cMailboxStatus> StatusAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StatusAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                var lMailboxCacheItem = mMailboxCache.CheckHandle(pHandle);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lHandle = mMailboxCache.SelectedMailbox?.Handle;
                    if (ReferenceEquals(pHandle, lHandle)) return lHandle.MailboxStatus;

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // status is msnunsafe

                    lCommand.BeginList(eListBracketing.none);
                    lCommand.Add(kStatusCommandPart);
                    lCommand.Add(lMailboxCacheItem.CommandPart);
                    lCommand.AddStatusAttributes(_Capability);
                    lCommand.EndList();

                    var lHook = new cStatusCommandHook(lMailboxCacheItem);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("status success");
                        return lMailboxCacheItem.MailboxStatus;
                    }

                    fCapabilities lTryIgnoring;
                    if (_Capability.CondStore) lTryIgnoring = fCapabilities.CondStore;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }

            private class cStatusCommandHook : cCommandHook
            {
                private readonly cMailboxCacheItem mMailboxCacheItem;
                private readonly int mSequence;

                public cStatusCommandHook(cMailboxCacheItem pMailboxCacheItem)
                {
                    mMailboxCacheItem = pMailboxCacheItem ?? throw new ArgumentNullException(nameof(pMailboxCacheItem));
                    mSequence = pMailboxCacheItem.MailboxCache.Sequence;
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusCommandHook), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok)
                        if (mMailboxCacheItem.Exists != false && (mMailboxCacheItem.Status == null || mMailboxCacheItem.Status.Sequence < mSequence))
                            mMailboxCacheItem.ResetExists(lContext);
                }
            }
        }
    }
}