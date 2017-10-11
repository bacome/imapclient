﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cStoreFeedback Store(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 1);
            var lFeedback = new cStoreFeedback(pHandle, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        public cStoreFeedback Store(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 2);
            var lFeedback = new cStoreFeedback(pHandles, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        public async Task<cStoreFeedback> StoreAsync(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 1);
            var lFeedback = new cStoreFeedback(pHandle, pOperation, pFlags);
            await ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        public async Task<cStoreFeedback> StoreAsync(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 2);
            var lFeedback = new cStoreFeedback(pHandles, pOperation, pFlags);
            await ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        private async Task ZStoreAsync(cStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZStoreAsync), pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException();

            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException();

            if (pFeedback.Count == 0) return;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                await lSession.StoreAsync(lMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}