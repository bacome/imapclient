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
            private static readonly cCommandPart kSortCommandPart = new cCommandPart("SORT ");

            public async Task<cHandleList> SortAsync(cMethodControl pMC, cMailboxId pMailboxId, cFilter pFilter, cSort pSort, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SortAsync), pMC, pMailboxId, pFilter, pSort);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);
                    lCommand.Add(await mSortExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // sort commands must be single threaded (so we can tell which result is which)

                    lCommand.Add(kSortCommandPart);
                    lCommand.Add(pSort);
                    lCommand.Add(cCommandPart.Space);
                    lCommand.Add(pFilter, true, EnabledExtensions, mEncoding); // if the filter has UIDs in it, this makes the command sensitive to UIDValidity changes

                    var lHook = new cSortCommandHook(_SelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.Result == cCommandResult.eResult.ok)
                    {
                        lContext.TraceInformation("sort success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.Sort, "results not received on a successful sort", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed sort");

                    if (lResult.Result == cCommandResult.eResult.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.Sort, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.Sort, lContext);
                }
            }

            private class cSortCommandHook : cCommandHook
            {
                private static readonly cBytes kSort = new cBytes("SORT");

                private readonly cSelectedMailbox mSelectedMailbox;
                private cUIntList mMSNs = null;

                public cSortCommandHook(cSelectedMailbox pSelectedMailbox)
                {
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSortCommandHook), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kSort)) return eProcessDataResult.notprocessed;

                    cUIntList lMSNs = new cUIntList();

                    while (true)
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;

                        if (!pCursor.GetNZNumber(out _, out var lMSN))
                        {
                            lContext.TraceWarning("likely malformed sort: not an nz-number list?");
                            return eProcessDataResult.notprocessed;
                        }

                        lMSNs.Add(lMSN);
                    }

                    if (!pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed sort: not at end?");
                        return eProcessDataResult.notprocessed;
                    }

                    if (mMSNs == null) mMSNs = lMSNs;
                    else mMSNs.AddRange(lMSNs);

                    return eProcessDataResult.processed;
                }

                public cHandleList Handles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSortCommandHook), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.Result == cCommandResult.eResult.ok && mMSNs != null)
                    {
                        cHandleList lHandles = new cHandleList();
                        foreach (var lMSN in mMSNs) lHandles.Add(mSelectedMailbox.GetHandle(lMSN));
                        Handles = lHandles;
                    }
                }
            }
        }
    }
}