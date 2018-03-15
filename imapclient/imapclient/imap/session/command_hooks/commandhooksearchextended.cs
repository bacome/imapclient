using System;
using System.Linq;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cCommandHookBaseSearchExtended : cCommandHook
            {
                private readonly cCommandTag mCommandTag;
                protected readonly cSelectedMailbox mSelectedMailbox;
                protected cSequenceSets mSequenceSets = null;

                public cCommandHookBaseSearchExtended(cCommandTag pCommandTag, cSelectedMailbox pSelectedMailbox)
                {
                    mCommandTag = pCommandTag ?? throw new ArgumentNullException(nameof(pCommandTag));
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                }

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookBaseSearchExtended), nameof(ProcessData));

                    if (!(pData is cResponseDataESearch lESearch)) return eProcessDataResult.notprocessed;
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

                public cMessageHandleList MessageHandles { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSearchExtended), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eCommandResultType.ok || mSequenceSets == null) return;
                    if (!cUIntList.TryConstruct(mSequenceSets, mSelectedMailbox.MessageCache.Count, !mSort, out var lMSNs)) return;
                    MessageHandles = new cMessageHandleList(lMSNs.Select(lMSN => mSelectedMailbox.GetHandle(lMSN)));
                }
            }
        }
    }
}