using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kSubscribeCommandPart = new cTextCommandPart("SUBSCRIBE ");
            private static readonly cCommandPart kUnsubscribeCommandPart = new cTextCommandPart("UNSUBSCRIBE ");

            public async Task SubscribeAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SubscribeAsync), pMC, pMailboxHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

                var lItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kSubscribeCommandPart, lItem.MailboxNameCommandPart);

                    lBuilder.Add(new cSubscribeCommandHook(lItem, true));

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("subscribe success");
                        return;
                    }

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, 0, lContext);
                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }

            public async Task UnsubscribeAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UnsubscribeAsync), pMC, pMailboxHandle);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

                var lItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kUnsubscribeCommandPart, lItem.MailboxNameCommandPart);

                    lBuilder.Add(new cSubscribeCommandHook(lItem, false));

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("unsubscribe success");
                        return;
                    }

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, 0, lContext);
                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cSubscribeCommandHook : cCommandHook
            {
                private readonly cMailboxCacheItem mItem;
                private readonly bool mWillBeSubscribedOnOK;
                private int mSequence;

                public cSubscribeCommandHook(cMailboxCacheItem pItem, bool pWillBeSubscribedOnOK)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                    mWillBeSubscribedOnOK = pWillBeSubscribedOnOK;
                }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSubscribeCommandHook), nameof(CommandStarted));
                    mSequence = mItem.MailboxCache.Sequence;
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSubscribeCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eIMAPCommandResultType.ok) mItem.SetLSubFlags(new cLSubFlags(mSequence, mWillBeSubscribedOnOK), lContext);
                }
            }
        }
    }
}