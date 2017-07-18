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
            private static readonly cCommandPart kListCommandPartList = new cCommandPart("LIST \"\" ");
            private static readonly cCommandPart kListCommandPartRList = new cCommandPart("RLIST \"\" ");

            public async Task ListAsync(cMethodControl pMC, cListPattern pPattern, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListAsync), pMC, pPattern);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (!mStringFactory.TryAsListMailbox(pPattern.ListMailbox, pPattern.Delimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pPattern));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    fCapabilities lTryIgnoring;

                    if (mMailboxReferrals && _Capability.MailboxReferrals)
                    {
                        lCommand.Add(kListCommandPartRList);
                        lTryIgnoring = fCapabilities.MailboxReferrals;
                    }
                    else
                    {
                        lCommand.Add(kListCommandPartList);
                        lTryIgnoring = 0;
                    }

                    lCommand.Add(lListMailboxCommandPart);

                    var lHook = new cCommandHookList(mMailboxCache, pPattern.MailboxNamePattern, cMailboxFlags.LastSequence);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("r/list success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}