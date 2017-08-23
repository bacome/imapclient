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
            private static readonly cCommandPart kRenameCommandPart = new cCommandPart("RENAME ");

            public async Task RenameAsync(cMethodControl pMC, iMailboxHandle pHandle, cMailboxName pMailboxName, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(RenameAsync), pMC, pHandle, pMailboxName);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

                var lItem = mMailboxCache.CheckHandle(pHandle);
                if (!mCommandPartFactory.TryAsMailbox(pMailboxName.Path, pMailboxName.Delimiter, out var lMailboxCommandPart, out _)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false));
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kRenameCommandPart, lItem.MailboxNameCommandPart, cCommandPart.Space, lMailboxCommandPart);

                    if (!pHandle.MailboxName.IsInbox) lBuilder.Add(new cRenameCommandHook(lItem));

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("rename success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cRenameCommandHook : cCommandHook
            {
                private readonly cMailboxCacheItem mItem;
                private int mSequence;

                public cRenameCommandHook(cMailboxCacheItem pItem)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cRenameCommandHook), nameof(CommandStarted));
                    mSequence = mItem.MailboxCache.Sequence;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cRenameCommandHook), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType == eCommandResultType.ok)
                    {
                        mItem.ResetExists(lContext);
                        if (mItem.MailboxName.Delimiter != null) mItem.MailboxCache.ResetExists(new cMailboxPathPattern(mItem.MailboxName.Path + mItem.MailboxName.Delimiter, "*", mItem.MailboxName.Delimiter), mSequence, lContext);
                    }
                }
            }
        }
    }
}