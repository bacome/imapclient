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
            private static readonly cCommandPart kListCommandPart = new cCommandPart("LIST \"\" ");

            public async Task<List<cMailboxName>> ListAsync(cMethodControl pMC, cListPattern pPattern, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListAsync), pMC, pPattern);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                cCommandPart.cFactory lFactory = new cCommandPart.cFactory((EnabledExtensions & fEnableableExtensions.utf8) != 0);
                if (!lFactory.TryAsListMailbox(pPattern.ListMailbox, pPattern.Delimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pPattern));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    // this command uses the same response as rlist, so wait for rlist to finish and stop rlist from running
                    lCommand.Add(await mListExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false));
                    lCommand.Add(await mRListExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); 

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kListCommandPart, lListMailboxCommandPart);

                    var lHook = new cCommandHookList(pPattern.MailboxNamePattern, EnabledExtensions);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("list success");

                        var lMailboxNames = new List<cMailboxName>();

                        foreach (var lResponse in lHook.Responses)
                        {
                            ;?; // store
                            // need the capability to do the store: have to say which flags are valid
                            //  and that might mean children (if not supported) and special use (if not supported) [thinking about this]



                            lMailboxNames.Add(lResponse.MailboxName);
                        }

                        return lMailboxNames;
                    }

                    if (lHook.Responses.Count != 0) lContext.TraceError("received mailboxes on a failed list");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}