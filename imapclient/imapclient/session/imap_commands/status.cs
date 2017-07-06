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
            private static readonly cCommandPart kStatusCommandPart = new cCommandPart("STATUS ");
            private static readonly cCommandPart kStatusCommandPartrfc3501Attributes = new cCommandPart(" (MESSAGES RECENT UIDNEXT UIDVALIDITY UNSEEN");
            private static readonly cCommandPart kStatusCommandPartHighestModSeq = new cCommandPart(" HIGHESTMODSEQ");

            public async Task<cMailboxStatus> StatusAsync(cMethodControl pMC, cMailboxId pMailboxId, int pCacheAgeMax, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StatusAsync), pMC, pMailboxId, pCacheAgeMax);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                cCommandPart.cFactory lFactory = new cCommandPart.cFactory((EnabledExtensions & fEnableableExtensions.utf8) != 0);
                if (!lFactory.TryAsMailbox(pMailboxId.MailboxName, out var lMailboxCommandPart, out var lEncodedMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxId));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    if (_SelectedMailbox != null && _SelectedMailbox.MailboxId == pMailboxId) return mMailboxCache.Item(pMailboxId).Status;

                    if (pCacheAgeMax > 0)
                    {
                        var lItem = mMailboxCache.Item(pMailboxId);
                        if (lItem.StatusAge <= pCacheAgeMax) return lItem.Status;
                    }

                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // status is msnunsafe

                    lCommand.Add(kStatusStatusCommandPart);
                    lCommand.Add(lMailboxCommandPart);
                    lCommand.Add(kStatusCommandPartrfc3501Attributes);
                    if (_Capability.CondStore) lCommand.Add(kStatusCommandPartHighestModSeq);
                    lCommand.Add(cCommandPart.RParen);

                    var lHook = new cStatusCommandHook(lEncodedMailboxName);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("status success");

                        var lStatus = lHook.Status;

                        if (lStatus == null || lStatus.Messages == null || lStatus.Recent == null || lStatus.Unseen == null) throw new cUnexpectedServerActionException(0, "status not received", lContext);

                        cMailboxStatus lMailboxStatus = new cMailboxStatus(lStatus.Messages.Value, lStatus.Recent.Value, lStatus.UIDNext ?? 0, 0, lStatus.UIDValidity ?? 0, lStatus.Unseen.Value, 0, lStatus.HighestModSeq ?? 0);

                        mMailboxCache.SetStatus(pMailboxId.MailboxName, lMailboxStatus);

                        return lMailboxStatus;
                    }

                    if (lHook.Status != null) lContext.TraceError("received status on a failed status");

                    fCapabilities lTryIgnoring;
                    if (_Capability.CondStore) lTryIgnoring = fCapabilities.CondStore;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }

            private class cStatusCommandHook : cCommandHook
            {
                private static readonly cBytes kStatusSpace = new cBytes("STATUS ");

                private string mEncodedMailboxName;

                public cStatusCommandHook(string pEncodedMailboxName)
                {
                    mEncodedMailboxName = pEncodedMailboxName;
                }

                public cStatus Status { get; private set; } = null;

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusCommandHook), nameof(ProcessData));

                    cResponseDataStatus lStatus;

                    if (pCursor.Parsed)
                    {
                        lStatus = pCursor.ParsedAs as cResponseDataStatus;
                        if (lStatus == null) return eProcessDataResult.notprocessed;
                    }
                    else
                    {
                        if (!pCursor.SkipBytes(kStatusSpace)) return eProcessDataResult.notprocessed;

                        if (!cResponseDataStatus.Process(pCursor, out lStatus, lContext))
                        {
                            lContext.TraceWarning("likely malformed status response");
                            return eProcessDataResult.notprocessed;
                        }
                    }

                    if (lStatus.EncodedMailboxName != mEncodedMailboxName) return eProcessDataResult.notprocessed;

                    Status = cStatus.Combine(lStatus.Status, Status);

                    return eProcessDataResult.observed;
                }
            }
        }
    }
}