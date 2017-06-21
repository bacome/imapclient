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
            private static readonly cCommandPart kFetchCommandPartSpaceBodyPeekLBracket = new cCommandPart(" BODY.PEEK[");
            private static readonly cCommandPart kFetchCommandPartSpaceBinaryPeekLBracket = new cCommandPart(" BINARY.PEEK[");
            private static readonly cCommandPart kFetchCommandPartHeader = new cCommandPart("HEADER");
            private static readonly cCommandPart kFetchCommandPartHeaderFields = new cCommandPart("HEADER.FIELDS(");
            private static readonly cCommandPart kFetchCommandPartHeaderFieldsNot = new cCommandPart("HEADER.FIELDS.NOT(");
            private static readonly cCommandPart kFetchCommandPartText = new cCommandPart("TEXT");
            private static readonly cCommandPart kFetchCommandPartMime = new cCommandPart("MIME");
            private static readonly cCommandPart kFetchCommandPartLessThan = new cCommandPart("<");
            private static readonly cCommandPart kFetchCommandPartGreaterThan = new cCommandPart(">");


            private async Task<cBytes> ZFetchBodyAsync(cMethodControl pMC, cMailboxId pMailboxId, iMessageHandle pHandle, bool pBinary, cSection pSection, uint pOrigin, uint pLength, cTrace.cContext pParentContext)
            {
                // the caller must have checked that the binary option and the section option are compatible

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchBodyAsync), pMC, pMailboxId, pHandle, pSection, pOrigin, pLength);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);
                    lCommand.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lCommand.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // resolve MSN
                    uint lMSN = _SelectedMailbox.GetMSN(pHandle);
                    if (lMSN == 0) throw new cInvalidMessageHandleException(lContext); // either expunged or the cache has changed

                    // build command

                    lCommand.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSN));

                    if (pBinary) lCommand.Add(kFetchCommandPartSpaceBinaryPeekLBracket);
                    else lCommand.Add(kFetchCommandPartSpaceBodyPeekLBracket);

                    if (pSection.Part != null)
                    {
                        lCommand.Add(cCommandPart.AsAtom(pSection.Part));
                        if (pSection.TextPart != cSection.eTextPart.all) lCommand.Add(cCommandPart.Dot);
                    }

                    switch (pSection.TextPart)
                    {
                        case cSection.eTextPart.header:

                            lCommand.Add(kFetchCommandPartHeader);
                            break;

                        case cSection.eTextPart.headerfields:

                            lCommand.Add(kFetchCommandPartHeaderFields);
                            lCommand.Add(pSection.HeaderFields);
                            lCommand.Add(cCommandPart.RParen);
                            break;

                        case cSection.eTextPart.headerfieldsnot:

                            lCommand.Add(kFetchCommandPartHeaderFieldsNot);
                            lCommand.Add(pSection.HeaderFields);
                            lCommand.Add(cCommandPart.RParen);
                            break;

                        case cSection.eTextPart.text:

                            lCommand.Add(kFetchCommandPartText);
                            break;

                        case cSection.eTextPart.mime:

                            lCommand.Add(kFetchCommandPartMime);
                            break;

                        default:

                            throw new cInternalErrorException(lContext);
                    }

                    lCommand.Add(kFetchCommandPartLessThan, new cCommandPart(pOrigin), cCommandPart.Dot, new cCommandPart(pLength), kFetchCommandPartGreaterThan);

                    ;?; // add a commandhook to capture the result

                    // go

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("fetch success");
                        return;
                    }

                    if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}