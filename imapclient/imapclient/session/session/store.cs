using System;
using System.Collections.Generic;
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
            public async Task<cMessageHandleList> StoreAsync(cMethodControl pMC, cMessageHandleList pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StoreAsync), pMC, pHandles, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pHandles.Count == 0) throw new ArgumentOutOfRangeException(nameof(pHandles));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !mCapabilities.CondStore) throw new InvalidOperationException();

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pHandles); // to be repeated inside the select lock

                if (pHandles.TrueForAll(h => h.UID != null))
                {
                    Dictionary<uint, cStoreFeedbackItem> lDictionary = new Dictionary<uint, cStoreFeedbackItem>();
                    foreach (var lHandle in pHandles) lDictionary[lHandle.UID.UID] = new cStoreFeedbackItem(lHandle);
                    await ZUIDStoreAsync(pMC, lSelectedMailbox.Handle, pHandles[0].UID.UIDValidity, lDictionary, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
                    return new cMessageHandleList(from i in lDictionary.Values where !i.Fetched || i.Modified select i.Handle);
                }
                else return await ZStoreAsync(pMC, pHandles, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }

            public async Task<cUIDList> UIDStoreAsync(cMethodControl pMC, iMailboxHandle pHandle, cUIDList pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDStoreAsync), pMC, pHandle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pUIDs));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !mCapabilities.CondStore) throw new InvalidOperationException();

                mMailboxCache.CheckIsSelectedMailbox(pHandle, pUIDs[0].UIDValidity); // to be repeated inside the select lock

                Dictionary<uint, cStoreFeedbackItem> lDictionary = new Dictionary<uint, cStoreFeedbackItem>();
                foreach (var lUID in pUIDs) lDictionary[lUID.UID] = new cStoreFeedbackItem(lUID);
                await ZUIDStoreAsync(pMC, pHandle, pUIDs[0].UIDValidity, lDictionary, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
                return new cUIDList(from i in lDictionary.Values where !i.Fetched || i.Modified select i.UID);
            }
        }
    }
}