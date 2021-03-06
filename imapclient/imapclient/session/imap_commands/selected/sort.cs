﻿using System;
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
            private static readonly cCommandPart kSortCommandPart = new cTextCommandPart("SORT ");

            public async Task<cMessageHandleList> SortAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SortAsync), pMC, pMailboxHandle, pFilter, pSort);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
                if (pSort == null) throw new ArgumentNullException(nameof(pSort));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    var lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pFilter.UIDValidity);

                    // special case
                    if (ReferenceEquals(pFilter, cFilter.None)) return new cMessageHandleList();

                    lBuilder.Add(await mSortExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // sort commands must be single threaded (so we can tell which result is which)
                    if (pFilter.ContainsMessageHandles) lBuilder.Add(await mMSNUnsafeBlock.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // wait until all commands that are msnunsafe complete, block all commands that are msnunsafe

                    lBuilder.AddUIDValidity(lSelectedMailbox.MessageCache.UIDValidity); // if a UIDValidity change happens while the command is running, disbelieve the results

                    lBuilder.Add(kSortCommandPart);
                    lBuilder.Add(pSort);
                    lBuilder.Add(cCommandPart.Space);
                    lBuilder.Add(pFilter, lSelectedMailbox, true, mEncodingPartFactory);

                    var lHook = new cSortCommandHook(lSelectedMailbox);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("sort success");
                        if (lHook.MessageHandles == null) throw new cUnexpectedServerActionException(fCapabilities.sort, "results not received on a successful sort", lContext);
                        return lHook.MessageHandles;
                    }

                    if (lHook.MessageHandles != null) lContext.TraceError("results received on a failed sort");

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.sort, lContext);
                    throw new cProtocolErrorException(lResult, fCapabilities.sort, lContext);
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

                public cMessageHandleList MessageHandles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cSortCommandHook), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType != eCommandResultType.ok || mMSNs == null) return;

                    cMessageHandleList lMessageHandles = new cMessageHandleList();
                    foreach (var lMSN in mMSNs) lMessageHandles.Add(mSelectedMailbox.GetHandle(lMSN));
                    MessageHandles = lMessageHandles;
                }
            }
        }
    }
}