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

            public async Task<iMailboxHandle> CreateAsync(cMethodControl pMC, cMailboxName pMailboxName, bool pAsFutureParent, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CreateAsync), pMC, pMailboxName, pAsFutureParent);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();
                if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
                if (pAsFutureParent && pMailboxName.Delimiter == null) throw new ArgumentOutOfRangeException(nameof(pAsFutureParent));

                string lMailboxPath;
                if (pAsFutureParent) lMailboxPath = pMailboxName.Path + pMailboxName.Delimiter.Value;
                else lMailboxPath = pMailboxName.Path;

                if (!mCommandPartFactory.TryAsMailbox(lMailboxPath, pMailboxName.Delimiter, out var lMailboxCommandPart, out _)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!mCapabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kCreateCommandPart, lMailboxCommandPart);

                    var lHook = new cCreateCommandHook(mMailboxCache, pMailboxName);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("create success");
                        return lHook.Handle;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cCreateCommandHook : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly cMailboxName mMailboxName;

                public cCreateCommandHook(cMailboxCache pCache, cMailboxName pMailboxName)
                {
                    mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                    mMailboxName = pMailboxName ?? throw new ArgumentNullException(nameof(pMailboxName));
                }

                public iMailboxHandle Handle { get; private set; }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCreateCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eCommandResultType.ok) Handle = mCache.Create(mMailboxName, lContext);
                }
            }
        }
    }
}