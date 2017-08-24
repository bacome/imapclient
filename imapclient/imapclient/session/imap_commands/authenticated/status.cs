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

            public async Task StatusAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StatusAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                var lItem = mMailboxCache.CheckHandle(pHandle);

                if (mStatusAttributes == 0) return;

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select: the status command is not supported on the selected mailbox

                    var lHandle = mMailboxCache.SelectedMailboxDetails?.Handle;
                    if (ReferenceEquals(pHandle, lHandle)) return;

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // status is msnunsafe

                    lBuilder.BeginList(eListBracketing.none);
                    lBuilder.Add(kStatusCommandPart);
                    lBuilder.Add(lItem.MailboxNameCommandPart);
                    lBuilder.AddStatusAttributes(mStatusAttributes);
                    lBuilder.EndList();

                    var lHook = new cStatusCommandHook(lItem);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("status success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no)
                    {
                        lContext.TraceInformation("status unsuccessful");
                        return;
                    }

                    fKnownCapabilities lTryIgnoring;
                    if (mCapabilities.CondStore) lTryIgnoring = fKnownCapabilities.condstore;
                    else lTryIgnoring = 0;

                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }

            private class cStatusCommandHook : cCommandHook
            {
                private readonly cMailboxCacheItem mItem;
                private int mSequence;

                public cStatusCommandHook(cMailboxCacheItem pItem)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusCommandHook), nameof(CommandStarted));
                    mSequence = mItem.MailboxCache.Sequence;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eCommandResultType.bad || mItem.Exists == false) return;
                    if (mItem.Status == null || mItem.Status.Sequence < mSequence) mItem.ResetExists(lContext);
                }
            }
        }
    }
}