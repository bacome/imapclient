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
            private static readonly cCommandPart kListMailboxesCommandPart = new cTextCommandPart("LIST \"\" ");

            public async Task<List<iMailboxHandle>> ListMailboxesAsync(cMethodControl pMC, string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListMailboxesAsync), pMC, pListMailbox, pDelimiter, pPattern);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.enabled && _ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
                if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

                if (!mCommandPartFactory.TryAsListMailbox(pListMailbox, pDelimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kListMailboxesCommandPart, lListMailboxCommandPart);

                    var lHook = new cCommandHookListMailboxes(mMailboxCache, pPattern);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("list mailboxes success");
                        return lHook.Handles;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}