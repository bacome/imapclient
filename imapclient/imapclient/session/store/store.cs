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
            public async Task StoreAsync(cMethodControl pMC, cStoreFeedback pFeedback, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StoreAsync), pMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pFeedback.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !mCapabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pFeedback); // to be repeated inside the select lock
                if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate); // to be repeated inside the select lock

                if (pFeedback.AllHaveUID)
                {
                    cStoreFeedbackCollector lFeedbackCollector = new cStoreFeedbackCollector(pFeedback);
                    await ZUIDStoreAsync(pMC, lSelectedMailbox.Handle, pFeedback[0].Handle.UID.UIDValidity, lFeedbackCollector, pOperation, pFlags, pIfUnchangedSinceModSeq, null, lContext).ConfigureAwait(false);
                }
                else await ZStoreAsync(pMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }

            public async Task UIDStoreAsync(cMethodControl pMC, iMailboxHandle pHandle, cUIDStoreFeedback pFeedback, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDStoreAsync), pMC, pHandle, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pFeedback.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !mCapabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

                uint lUIDValidity = pFeedback[0].UID.UIDValidity;

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, lUIDValidity); // to be repeated inside the select lock
                if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate); // to be repeated inside the select lock

                cStoreFeedbackCollector lFeedbackCollector = new cStoreFeedbackCollector(pFeedback);
                await ZUIDStoreAsync(pMC, pHandle, lUIDValidity, lFeedbackCollector, pOperation, pFlags, pIfUnchangedSinceModSeq, pFeedback, lContext).ConfigureAwait(false);
            }
        }
    }
}