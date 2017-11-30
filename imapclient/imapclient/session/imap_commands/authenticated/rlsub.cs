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
            private static readonly cCommandPart kRLSubCommandPart = new cTextCommandPart("RLSUB \"\" ");

            public async Task<List<iMailboxHandle>> RLSubAsync(cMethodControl pMC, string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, bool pHasSubscribedChildren, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(RLSubAsync), pMC, pListMailbox, pDelimiter, pPattern, pHasSubscribedChildren);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
                if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

                if (!mCommandPartFactory.TryAsListMailbox(pListMailbox, pDelimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kRLSubCommandPart, lListMailboxCommandPart);

                    var lHook = new cCommandHookLSub(mMailboxCache, pPattern, pHasSubscribedChildren);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("rlsub success");
                        return lHook.MailboxHandles;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.mailboxreferrals, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.mailboxreferrals, lContext);
                }
            }
        }
    }
}