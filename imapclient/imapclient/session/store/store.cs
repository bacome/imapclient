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
            public async Task StoreAsync(cMethodControl pMC, cStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
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
                }
                else await ZStoreAsync(pMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }

            public async Task UIDStoreAsync(cMethodControl pMC, iMailboxHandle pHandle, cUIDStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDStoreAsync), pMC, pHandle, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pFeedback.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !mCapabilities.CondStore) throw new InvalidOperationException();

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, pFeedback[0].UID.UIDValidity); // to be repeated inside the select lock
                if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(); // to be repeated inside the select lock

                cStoreFeedbacker lFeedbacker = new cStoreFeedbacker(pFeedback);
                await ZUIDStoreAsync(pMC, pHandle, pFeedback[0].UID.UIDValidity, lFeedbacker, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}