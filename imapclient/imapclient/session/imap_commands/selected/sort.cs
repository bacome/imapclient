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

            public async Task<cMessageHandleList> SortAsync(cMethodControl pMC, iMailboxHandle pHandle, cFilter pFilter, cSort pSort, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SortAsync), pMC, pHandle, pFilter, pSort);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.selected) throw new InvalidOperationException();
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pSort == null) throw new ArgumentNullException(nameof(pSort));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = ZCheckHandle(pHandle);

                    lCommand.Add(await mSortExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // sort commands must be single threaded (so we can tell which result is which)

                    lCommand.Add(kSortCommandPart);
                    lCommand.Add(pSort);
                    lCommand.Add(cCommandPart.Space);
                    lCommand.Add(pFilter, true, EnabledExtensions, mEncoding); // if the filter has UIDs in it, this makes the command sensitive to UIDValidity changes

                    var lHook = new cSortCommandHook(lSelectedMailbox);
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("sort success");
                        if (lHook.Handles == null) throw new cUnexpectedServerActionException(fCapabilities.Sort, "results not received on a successful sort", lContext);
                        return lHook.Handles;
                    }

                    if (lHook.Handles != null) lContext.TraceError("results received on a failed sort");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.Sort, lContext);
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

                public cMessageHandleList Handles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSortCommandHook), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.ResultType == eCommandResultType.ok && mMSNs != null)
                    {
                        cMessageHandleList lHandles = new cMessageHandleList();
                        foreach (var lMSN in mMSNs) lHandles.Add(mSelectedMailbox.GetHandle(lMSN));
                        Handles = lHandles;
                    }
                }
            }
        }
    }
}