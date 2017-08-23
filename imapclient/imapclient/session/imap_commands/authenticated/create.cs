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
            private static readonly cCommandPart kCreateCommandPart = new cCommandPart("CREATE ");

            public async Task CreateAsync(cMethodControl pMC,  string pMailboxPath, char? pDelimiter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CreateAsync), pMC, pMailboxPath);

                ;?; // the mailboxname needs to be pased in ...

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pMailboxPath == null) throw new ArgumentNullException(nameof(pMailboxPath));

                if (!mCommandPartFactory.TryAsMailbox(pMailboxPath, pDelimiter, out var lMailboxCommandPart, out _)) throw new ArgumentOutOfRangeException(nameof(pMailboxPath));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false));
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kCreateCommandPart, lMailboxCommandPart);

                    lBuilder.Add(new cCreateCommandHook(lItem));

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("create success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cCreateCommandHook : cCommandHook
            {
                private readonly cMailboxCacheItem mItem;

                public cCreateCommandHook(cMailboxCacheItem pItem)
                {
                    mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCreateCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eCommandResultType.ok) mItem.Created(lContext);
                }
            }
        }
    }
}