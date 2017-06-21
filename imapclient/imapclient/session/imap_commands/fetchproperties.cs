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
            private static readonly cCommandPart kFetchCommandPartFetchSpace = new cCommandPart("FETCH ");

            private async Task ZFetchAsync(cMethodControl pMC, cMailboxId pMailboxId, cHandleList pHandles, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                // note that this silently fails if the handles are out of date
                //  AND if a UID validity change were to happen during the run it wouldn't complain either
                //
                // note that the caller should have checked that pHandles is non-null and contains no null entries and that pProperties contains some properties to fetch

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchAsync), pMC, pMailboxId, pHandles, pProperties);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);
                    lCommand.Add(await mPipeline.GetIdleBlockTokenAsync(pMC, lContext).ConfigureAwait(false)); // stop the pipeline from iding (idle is msnunsafe)
                    lCommand.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    // resolve MSNs

                    cUIntList lMSNs = new cUIntList();

                    foreach (var lHandle in pHandles)
                    {
                        var lMSN = _SelectedMailbox.GetMSN(lHandle);
                        if (lMSN != 0) lMSNs.Add(lMSN);
                    }

                    if (lMSNs.Count == 0) return;

                    // build command

                    lCommand.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSNs.ToSequenceSet()), cCommandPart.Space);
                    lCommand.Add(pProperties);

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

            /*
            private Task<int> ZFetchStreamAsync(cMethodControl pMC, cMailboxId pMailboxId, iMessageProperties pHandle, cSection pSection, cMessagePart pPart, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                // returns the number of bytes written
                //  if the handle is out of date this will throw an invalid handle exception
                //  if the UID validity changes whilst this is running this will throw a uid validity changed exception
                //
                // note that the caller should have checked that pHandle is non-null

                var lContext = pParentContext.NewMethodV(nameof(cSession), nameof(FetchAsync), pMC, pMailboxId, pHandle, pSection, pPart, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (State != eState.selected) throw new InvalidOperationException("must be selected");

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new InvalidOperationException("account not connected");

                bool lUseBinary = (_Capability.Binary && (pSection == null || pSection.TextPart == cSection.eTextPart.all) && pDecoding != eDecodingRequired.none);

                eDecodingRequired lDecoding;

                if (lUseBinary) lDecoding = eDecodingRequired.none;
                else
                {
                    if (pDecoding == eDecodingRequired.unknown) throw new x;
                    lDecoding = pDecoding;
                }

                if (pHandle.UID == null) return ZFetchAsync(pMC, pMailboxId, pHandle, pSection, pPart, lUseBinary, lDecoding, pStream, lContext);
                else return ZFetchUIDAsync(pMC, pMailboxId, pHandle, pSection, pPart, lUseBinary, lDecoding, pStream, lContext);
            }

            private async Task<int> ZFetchStreamAsync(cMethodControl pMC, cMailboxId pMailboxId, iMessageProperties pHandle, cSection pSection, cMessagePart pPart, bool pUseBinary, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethodV(nameof(cSession), nameof(ZFetchAsync), pMC, pMailboxId, pHandle, pSection, pPart, pUseBinary, pDecoding);

                cFetchCommandHook lCommandHook;
                fCapabilities lTryIgnoring;
                Task<cCommandResult> lTask;

                // check the selected mailbox and submit the command whilst holding select exclusive access
                //  once the command is in the pipeline we know that it must run before the selected mailbox changes, so we can release select exclusive access
                //
                using (var lSelectAccess = await mSelectExclusiveAccess.GetAccessAsync(pMC, lContext).ConfigureAwait(false))
                {
                    // check the selected mailbox
                    //
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new InvalidOperationException("mailbox not selected");

                    // resolve the MSN and submit the command whilst holding execute exclusive access
                    //  once the command is in the pipeline we know that it must run before any msnunsafe commands, so we can release the execute exclusive access
                    //
                    // (NOTE that the order in which the locks are attained is important to avoid deadlock)
                    //
                    using (var lExecuteAccess = await mCommandPipeline.GetExecuteAccessAsync(pMC, cCommandPipeline.eAccessType.allowmsnsafecommands, lContext).ConfigureAwait(false))
                    {
                        // must capture the uidvalidity before resolving the MSN (because it can change at any time)
                        uint? lUIDValidity = _SelectedMailbox.UIDValidity;

                        // resolve the MSN
                        var lMSN = _SelectedMailbox.GetMSN(pHandle);
                        if (lMSN == 0) throw new cMessageHandleInvalid();

                        // build the command

                        cCommandBuilder lBuilder = new cCommandBuilder();

                        if (pUseBinary)
                        {
                            lBuilder.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSN), kFetchCommandPartSpaceBinaryPeekLBracket);
                            lTryIgnoring = fCapabilities.Binary;
                        }
                        else
                        {
                            lBuilder.Add(kFetchCommandPartFetchSpace, new cCommandPart(lMSN), kFetchCommandPartSpaceBodyPeekLBracket);
                            lTryIgnoring = 0;
                        }

                        if (pSection != null) lBuilder.Add(new cCommandPart(pSection));
                        lBuilder.Add(cCommandPart.RBracket);
                        if (pPart != null) lBuilder.Add(new cCommandPart(pPart));

                        // create command hook
                        lCommandHook = new cFetchCommandHook(cFetchCommandHook.eIdType.msn, lMSN, pUseBinary, pSection, pPart?.Origin);

                        // submit command
                        //
                        lTask = mCommandPipeline.ExecuteMSNSafeAsync(pMC, lExecuteAccess, new cCommandText(lBuilder.Parts), lCommandHook, lUIDValidity, lContext);
                    }
                }

                var lResult = await lTask.ConfigureAwait(false);

                if (lResult.Result == cCommandResult.eResult.ok)
                {
                    lContext.TraceInformation("fetch command success");

                    // should be a common

                    switch (pDecoding)
                    {
                        case eDecodingRequired.base64:

                            throw new NotImplementedException();

                        case eDecodingRequired.quotedprintable:


                    }

                    ;?; // write the data to the stream (possibly decoding it first)

                    return;
                }

                if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
            }

            private async Task<int> ZFetchStreamUIDAsync(cMethodControl pMC, cMailboxId pMailboxId, iMessageProperties pHandle, cSection pSection, cMessagePart pPart, bool pUseBinary, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethodV(nameof(cSession), nameof(ZFetchUIDAsync), pMC, pMailboxId, pHandle, pSection, pPart, pUseBinary, pDecoding);

                cFetchCommandHook lCommandHook;
                fCapabilities lTryIgnoring;
                Task<cCommandResult> lTask;

                // check the selected mailbox and submit the command whilst holding select exclusive access
                //  once the command is in the pipeline we know that it must run before the selected mailbox changes, so we can release select exclusive access
                //
                using (var lSelectAccess = await mSelectExclusiveAccess.GetAccessAsync(pMC, lContext).ConfigureAwait(false))
                {
                    // check the selected mailbox
                    //
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new InvalidOperationException("mailbox not selected");

                    // build command
                    
                    cCommandBuilder lBuilder = new cCommandBuilder();

                    if (pUseBinary)
                    {
                        lBuilder.Add(kFetchCommandPartUIDFetchSpace, new cCommandPart(pHandle.UID.UID), kFetchCommandPartSpaceBinaryPeekLBracket);
                        lTryIgnoring = fCapabilities.Binary;
                    }
                    else
                    {
                        lBuilder.Add(kFetchCommandPartUIDFetchSpace, new cCommandPart(pHandle.UID.UID), kFetchCommandPartSpaceBodyPeekLBracket);
                        lTryIgnoring = 0;
                    }

                    if (pSection != null) lBuilder.Add(new cCommandPart(pSection));
                    lBuilder.Add(cCommandPart.RBracket);
                    if (pPart != null) lBuilder.Add(new cCommandPart(pPart));

                    // create command hook
                    lCommandHook = new cFetchCommandHook(cFetchCommandHook.eIdType.uid, pHandle.UID.UID, pUseBinary, pSection, pPart?.Origin);

                    // submit command
                    //
                    lTask = mCommandPipeline.ExecuteMSNUnsafeAsync(pMC, new cCommandText(lBuilder.Parts), lCommandHook, pHandle.UID.UIDValidity, lContext);
                }

                var lResult = await lTask.ConfigureAwait(false);

                if (lResult.Result == cCommandResult.eResult.ok)
                {
                    lContext.TraceInformation("uid fetch command success");

                    ;?; // write the data to the stream (possibly decoding it first)

                    return;
                }

                if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
            }

            private class cFetchCommandHook
            {
                public enum eIdType { msn, uid }

                private readonly eIdType mIdType;
                private readonly uint mId;
                private readonly bool mBinary;
                private readonly cSection mSection;
                private readonly uint? mOrigin;
                public readonly cBytes Bytes;

                public cFetchCommandHook(eIdType pIdType, uint pId, bool pBinary, cSection pSection, uint? pOrigin)
                {
                    mIdType = pIdType;
                    mId = pId;
                    mBinary = pBinary;
                    mSection = pSection;
                    mOrigin = pOrigin;
                }

                ;?; // process

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSearchCommandHook), nameof(CommandCompleted), pResult, pException);

                    mSearchAccess.Dispose();

                    if (pResult.Result == cCommandResult.eResult.ok && mMSNs != null)
                    {
                        List<iMessageProperties> lMessages = new List<iMessageProperties>();
                        foreach (var lMSN in mMSNs.ToSortedUniqueList()) lMessages.Add(mSelectedMailbox.GetMessageHandle(lMSN));
                        Messages = lMessages;
                    }
                }
            } */
        }
    }
}