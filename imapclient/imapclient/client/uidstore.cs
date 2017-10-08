using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cUIDStoreFeedbackItem UIDStore(iMailboxHandle pHandle, cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            ;?;
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));



            var lTask = ZUIDStoreAsync(pHandle, cUIDList.FromUID(pUID), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result.Count == 0;
        }

        public bool UIDStore(iMailboxHandle pHandle, IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lTask = ZUIDStoreAsync(pHandle, cUIDList.FromUIDs(pUIDs), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public async Task<bool> UIDStoreAsync(iMailboxHandle pHandle, cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFailedToStore = await ZUIDStoreAsync(pHandle, cUIDList.FromUID(pUID), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFailedToStore.Count == 0;
        }

        public Task<cUIDList> UIDStoreAsync(iMailboxHandle pHandle, IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            return ZUIDStoreAsync(pHandle, cUIDList.FromUIDs(pUIDs), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
        }

        private async Task ZUIDStoreAsync(iMailboxHandle pHandle, cUIDList pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDStoreAsync), pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException();

            if (pUIDs.Count == 0) return pUIDs;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                return await lSession.UIDStoreAsync(lMC, pHandle, pUIDs, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}