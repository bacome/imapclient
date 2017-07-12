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
            private static readonly cCommandPart kLSubCommandPart = new cCommandPart("LSUB \"\" ");

            public async Task LSubAsync(cMethodControl pMC, cListPattern pPattern, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(LSubAsync), pMC, pPattern);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                cCommandPart.cFactory lFactory = new cCommandPart.cFactory((EnabledExtensions & fEnableableExtensions.utf8) != 0);
                if (!lFactory.TryAsListMailbox(pPattern.ListMailbox, pPattern.Delimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pPattern));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kLSubCommandPart, lListMailboxCommandPart);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("lsub success");
                        return;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}