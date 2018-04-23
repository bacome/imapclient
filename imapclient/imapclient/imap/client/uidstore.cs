using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cUIDStoreFeedback UIDStore(iMailboxHandle pMailboxHandle, cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUID, pOperation, pFlags);
            var lTask = ZUIDStoreAsync(pMailboxHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        internal cUIDStoreFeedback UIDStore(iMailboxHandle pMailboxHandle, IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUIDs, pOperation, pFlags);
            var lTask = ZUIDStoreAsync(pMailboxHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        internal async Task<cUIDStoreFeedback> UIDStoreAsync(iMailboxHandle pMailboxHandle, cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUID, pOperation, pFlags);
            await ZUIDStoreAsync(pMailboxHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        internal async Task<cUIDStoreFeedback> UIDStoreAsync(iMailboxHandle pMailboxHandle, IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUIDs, pOperation, pFlags);
            await ZUIDStoreAsync(pMailboxHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        private async Task ZUIDStoreAsync(iMailboxHandle pMailboxHandle, cUIDStoreFeedback pFeedback, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDStoreAsync), pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

            if (pFeedback.Count == 0) return;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                await lSession.UIDStoreAsync(lMC, pMailboxHandle, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}