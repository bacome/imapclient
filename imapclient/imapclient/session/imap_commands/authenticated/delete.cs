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
            private static readonly cCommandPart kDeleteCommandPart = new cTextCommandPart("DELETE ");

            public async Task DeleteAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(DeleteAsync), pMC, pHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                var lItem = mMailboxCache.CheckHandle(pHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!mCapabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kDeleteCommandPart, lItem.MailboxNameCommandPart);

                    lBuilder.Add(new cDeleteCommandHook(lItem));

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("delete success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cDeleteCommandHook : cCommandHook
            {
                private readonly cMailboxCacheItem mItem;

                public cDeleteCommandHook(cMailboxCacheItem pItem)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cDeleteCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eCommandResultType.ok) mItem.ResetExists(lContext);
                }
            }
        }
    }
}