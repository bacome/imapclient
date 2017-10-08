using System;
using System.Linq;
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
            public async Task<bool> StoreAsync(cMethodControl pMC, cStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StoreAsync), pMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pFeedback.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !mCapabilities.CondStore) throw new InvalidOperationException();

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pFeedback); // to be repeated inside the select lock
                if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(); // to be repeated inside the select lock

                if (pFeedback.TrueForAll(i => i.Handle.UID != null))
                {
                    cStoreFeedbacker lFeedbacker = new cStoreFeedbacker(pFeedback);
                    await ZUIDStoreAsync(pMC, lSelectedMailbox.Handle, pFeedback[0].Handle.UID.UIDValidity, lFeedbacker, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
                    // note: if some of the messages were deleted then this will succeed

                    ;?; // now for each handle check that it has had the operation done: 
                    // if ANY has modified, return false
                    //  if ANY doesn't have the falgs set as we think they shoul dbe return false
                    // NOTE: if any message was deleted this won't fail, where as the standard store will fail

                }
                else return await ZStoreAsync(pMC, pHandles, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }

            public async Task<cUIDList> UIDStoreAsync(cMethodControl pMC, iMailboxHandle pHandle, cUIDList pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDStoreAsync), pMC, pHandle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pUIDs));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !mCapabilities.CondStore) throw new InvalidOperationException();

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, pUIDs[0].UIDValidity); // to be repeated inside the select lock
                if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(); // to be repeated inside the select lock

                cStoreFeedback lFeedback = new cStoreFeedback(true);
                foreach (var lUID in pUIDs) lFeedback.Add(lUID);
                await ZUIDStoreAsync(pMC, pHandle, pUIDs[0].UIDValidity, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
                return new cUIDList(from i in lFeedback.Items where !i.Fetched || i.Modified select i.UID);
            }
        }
    }
}