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
        /// <summary>
        /// Intended for use by the <see cref="cMailbox"/> class.
        /// </summary>
        /// <param name="pHandle"></param>
        /// <param name="pUID"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public cUIDStoreFeedback UIDStore(iMailboxHandle pHandle, cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUID, pOperation, pFlags);
            var lTask = ZUIDStoreAsync(pHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        /// <summary>
        /// Intended for use by the <see cref="cMailbox"/> class.
        /// </summary>
        /// <param name="pHandle"></param>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public cUIDStoreFeedback UIDStore(iMailboxHandle pHandle, IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUIDs, pOperation, pFlags);
            var lTask = ZUIDStoreAsync(pHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        /// <summary>
        /// Intended for use by the <see cref="cMailbox"/> class.
        /// </summary>
        /// <param name="pHandle"></param>
        /// <param name="pUID"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public async Task<cUIDStoreFeedback> UIDStoreAsync(iMailboxHandle pHandle, cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUID, pOperation, pFlags);
            await ZUIDStoreAsync(pHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        /// <summary>
        /// Intended for use by the <see cref="cMailbox"/> class.
        /// </summary>
        /// <param name="pHandle"></param>
        /// <param name="pUIDs"></param>
        /// <param name="pOperation"></param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        public async Task<cUIDStoreFeedback> UIDStoreAsync(iMailboxHandle pHandle, IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDStore));
            var lFeedback = new cUIDStoreFeedback(pUIDs, pOperation, pFlags);
            await ZUIDStoreAsync(pHandle, lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        private async Task ZUIDStoreAsync(iMailboxHandle pHandle, cUIDStoreFeedback pFeedback, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDStoreAsync), pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException();

            if (pFeedback.Count == 0) return;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                await lSession.UIDStoreAsync(lMC, pHandle, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}