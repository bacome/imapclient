using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cCommandHookBaseSearchExtended : cCommandHook
            {
                private static readonly cBytes kESearch = new cBytes("ESEARCH");

                private readonly cCommandTag mCommandTag;
                protected readonly cSelectedMailbox mSelectedMailbox;
                protected cSequenceSets mSequenceSets = null;

                public cCommandHookBaseSearchExtended(cCommandTag pCommandTag, cSelectedMailbox pSelectedMailbox)
                {
                    mCommandTag = pCommandTag ?? throw new ArgumentNullException(nameof(pCommandTag));
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                }

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookBaseSearchExtended), nameof(ProcessData));

                    cResponseDataESearch lESearch;

                    if (pCursor.Parsed)
                    {
                        lESearch = pCursor.ParsedAs as cResponseDataESearch;
                        if (lESearch == null) return eProcessDataResult.notprocessed;
                    }
                    else
                    {
                        if (!pCursor.SkipBytes(kESearch)) return eProcessDataResult.notprocessed;

                        if (!cResponseDataESearch.Process(pCursor, out lESearch, lContext))
                        {
                            lContext.TraceWarning("likely malformed esearch response");
                            return eProcessDataResult.notprocessed;
                        }
                    }

                    if (!cASCII.Compare(mCommandTag, lESearch.Tag, true)) return eProcessDataResult.notprocessed;

                    if (mSequenceSets == null) mSequenceSets = new cSequenceSets();
                    if (lESearch.SequenceSet != null) mSequenceSets.Add(lESearch.SequenceSet);

                    return eProcessDataResult.processed;
                }
            }

            private class cCommandHookSearchExtended : cCommandHookBaseSearchExtended
            {
                private readonly bool mSort; // if the results are coming sorted this should be set to true

                public cCommandHookSearchExtended(cCommandTag pCommandTag, cSelectedMailbox pSelectedMailbox, bool pSort) : base(pCommandTag, pSelectedMailbox)
                {
                    mSort = pSort;
                }

                public cHandleList Handles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSearchExtended), nameof(CommandCompleted), pResult, pException);

                    if (pResult != null && pResult.Result == cCommandResult.eResult.ok && mSequenceSets != null)
                    {
                        var lMSNs = cUIntList.FromSequenceSets(mSequenceSets, (uint)mSelectedMailbox.Messages);
                        if (!mSort) lMSNs = lMSNs.ToSortedUniqueList();
                        cHandleList lHandles = new cHandleList();
                        foreach (var lMSN in lMSNs) lHandles.Add(mSelectedMailbox.GetHandle(lMSN));
                        Handles = lHandles;
                    }
                }
            }
        }
    }
}