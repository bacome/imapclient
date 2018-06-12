using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kCloseCommandPart = new cTextCommandPart("CLOSE");

            public async Task<IEnumerable<cUID>> CloseAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(CloseAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // get exclusive access to the selected mailbox

                    mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, null);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kCloseCommandPart);

                    var lHook = new cCloseCommandHook(mMailboxCache);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("close success");
                        return lHook.KnownDeletedMessageUIDs;
                    }

                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cCloseCommandHook : cCommandHook
            {
                private readonly cMailboxCache mMailboxCache;

                public cCloseCommandHook(cMailboxCache pMailboxCache)
                {
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                }

                public IEnumerable<cUID> KnownDeletedMessageUIDs { get; private set; } = null;

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCloseCommandHook), nameof(CommandStarted));
                    KnownDeletedMessageUIDs = mMailboxCache.GetKnownDeletedMessageUIDs(lContext);
                }

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCloseCommandHook), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType == eIMAPCommandResultType.ok) mMailboxCache.Unselect(lContext);
                }
            }
        }
    }
}