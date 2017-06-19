using System;
using System.Collections.Generic;
using System.IO;
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
            private static readonly cCommandPart kUIDFetchCommandPartUIDFetchSpace = new cCommandPart("UID FETCH ");
            private static readonly cCommandPart kUIDFetchCommandPartSpaceBodyPeekLBracket = new cCommandPart(" BODY.PEEK[");
            private static readonly cCommandPart kUIDFetchCommandPartSpaceBinaryPeekLBracket = new cCommandPart(" BINARY.PEEK[");

            private async Task ZUIDFetchPropertiesAsync(cMethodControl pMC, cMailboxId pMailboxId, uint pUIDValidity, cUIntList pUIDs, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                // note that this will fail if the UIDValidity has changed (this is different to the behaviour of standard fetch)
                // note that the caller should have checked that pProperties contains some properties to fetch

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchPropertiesAsync), pMC, pMailboxId, pUIDValidity, pUIDs, pProperties);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kUIDFetchCommandPartUIDFetchSpace, new cCommandPart(pUIDs.ToSequenceSet()), cCommandPart.Space);
                    lCommand.Add(pProperties);

                    lCommand.UIDValidity = pUIDValidity; // the command is sensitive to UIDValidity changes

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("uid fetch success");
                        return;
                    }

                    if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}