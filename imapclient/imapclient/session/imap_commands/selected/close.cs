﻿using System;
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
            private static readonly cCommandPart kCloseCommandPart = new cCommandPart("CLOSE");

            public async Task CloseAsync(cMethodControl pMC, iMailboxHandle pHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CloseAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    mMailboxCache.CheckIsSelectedMailbox(pHandle);

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kCloseCommandPart);

                    var lHook = new cCloseCommandHook(mMailboxCache);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("close success");
                        return;
                    }

                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cCloseCommandHook : cCommandHook
            {
                private readonly cMailboxCache mMailboxCache;

                public cCloseCommandHook(cMailboxCache pMailboxCache)
                {
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                }

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCloseCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok) mMailboxCache.Deselect(lContext);
                }
            }
        }
    }
}