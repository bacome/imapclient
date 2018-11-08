using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;
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
                protected cSequenceSets mSequenceSets = null;

                public cCommandHookBaseSearchExtended(cCommandTag pCommandTag)
                {
                    mCommandTag = pCommandTag ?? throw new ArgumentNullException(nameof(pCommandTag));
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
                private readonly cSelectedMailbox mSelectedMailbox;
                private readonly bool mSort; // if the results are coming sorted this should be set to true

                public cCommandHookSearchExtended(cCommandTag pCommandTag, cSelectedMailbox pSelectedMailbox, bool pSort) : base(pCommandTag)
                {
                    mSelectedMailbox = pSelectedMailbox;
                    mSort = pSort;
                }

                public cMessageHandleList MessageHandles { get; private set; } = null;

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookSearchExtended), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eIMAPCommandResultType.ok || mSequenceSets == null) return;
                    if (!cUIntList.TryConstruct(mSequenceSets, (uint)mSelectedMailbox.MessageCache.Count, !mSort, out var lMSNs)) return;
                    // NOTE: the collection must be rendered, IEnumerable is not safe as evaluation of it can be delayed
                    MessageHandles = new cMessageHandleList(lMSNs.Select(lMSN => mSelectedMailbox.GetHandle(lMSN)));
                }
            }

            private class cCommandHookUIDSearchExtended : cCommandHookBaseSearchExtended
            {
                private readonly uint mUIDValidity;
                private readonly bool mSort; // if the results are coming sorted this should be set to true

                public cCommandHookUIDSearchExtended(cCommandTag pCommandTag, uint pUIDValidity, bool pSort) : base(pCommandTag)
                {
                    mUIDValidity = pUIDValidity;
                    mSort = pSort;
                }

                public IEnumerable<cUID> UIDs { get; private set; } = null;

                public override void CommandCompleted(cIMAPCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookUIDSearchExtended), nameof(CommandCompleted), pResult);
                    if (pResult.ResultType != eIMAPCommandResultType.ok || mSequenceSets == null) return;
                    if (!cUIntList.TryConstruct(mSequenceSets, 0, !mSort, out var lUIDs)) return;
                    UIDs = lUIDs.Select(lUID => new cUID(mUIDValidity, lUID));
                }
            }
        }
    }
}