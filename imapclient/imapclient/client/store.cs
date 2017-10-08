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
        public cStoreFeedbackItem Store(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            var lTask = ZStoreAsync(cMessageHandleList.FromHandle(pHandle), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result.Count == 0;
        }

        public bool Store(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            var lTask = ZStoreAsync(cMessageHandleList.FromHandles(pHandles), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public async Task<bool> StoreAsync(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            var lFailedToStore = await ZStoreAsync(cMessageHandleList.FromHandle(pHandle), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFailedToStore.Count == 0;
        }

        public Task<bool> StoreAsync(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            return ZStoreAsync(cMessageHandleList.FromHandles(pHandles), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
        }

        private async Task<bool> ZStoreAsync(cStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZStoreAsync), pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException();

            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException();

            if (pFeedback.Count == 0) return true;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                return await lSession.StoreAsync(lMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}