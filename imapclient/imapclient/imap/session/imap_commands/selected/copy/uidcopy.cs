using System;
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
            private static readonly cCommandPart kUIDCopyCommandPart = new cTextCommandPart("UID COPY ");

            private async Task<cCopyFeedback> ZUIDCopyAsync(cMethodControl pMC, iMailboxHandle pSourceMailboxHandle, uint pSourceUIDValidity, cUIntList pSourceUIDs, cMailboxCacheItem pDestinationItem, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDCopyAsync), pMC, pSourceMailboxHandle, pSourceUIDValidity, pSourceUIDs, pDestinationItem);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pSourceMailboxHandle, pSourceUIDValidity);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.AddUIDValidity(pSourceUIDValidity); // the command is sensitive to UIDValidity changes

                    // build the command
                    lBuilder.Add(kUIDCopyCommandPart, new cTextCommandPart(cSequenceSet.FromUInts(pSourceUIDs)), cCommandPart.Space, pDestinationItem.MailboxNameCommandPart);

                    // add the hook
                    var lHook = new cCommandHookCopy(pSourceUIDValidity);
                    lBuilder.Add(lHook);

                    // submit the command                
                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid copy success");
                        return lHook.Feedback;
                    }

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, 0, lContext);
                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}