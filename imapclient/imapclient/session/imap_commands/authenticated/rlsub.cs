using System;
using System.Collections.Generic;
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
            private static readonly cCommandPart kRLSubCommandPart = new cCommandPart("RLSUB \"\" ");

            public async Task<List<iMailboxHandle>> RLSubAsync(cMethodControl pMC, string pListMailbox, char? pDelimiter, cMailboxNamePattern pPattern, bool pHasSubscribedChildren, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(RLSubAsync), pMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();
                if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
                if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

                if (!mCommandPartFactory.TryAsListMailbox(pListMailbox, pDelimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kRLSubCommandPart, lListMailboxCommandPart);

                    var lHook = new cCommandHookLSub(mMailboxCache, pPattern, pHasSubscribedChildren);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("rlsub success");
                        return lHook.Handles;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.MailboxReferrals, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.MailboxReferrals, lContext);
                }
            }
        }
    }
}