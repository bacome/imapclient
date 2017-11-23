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
        internal cStoreFeedback Store(iMessageHandle pMessageHandle, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 1);
            var lFeedback = new cStoreFeedback(pMessageHandle, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        internal cStoreFeedback Store(IEnumerable<iMessageHandle> pMessageHandles, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 2);
            var lFeedback = new cStoreFeedback(pMessageHandles, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        /// <summary>
        /// Stores flags for a set of messages. The mailbox that the messages are in must be selected.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        /// <remarks>
        /// <paramref name="pIfUnchangedSinceModSeq"/> may only be specified if the containing mailbox's <see cref="cMailbox.HighestModSeq"/> is not zero. 
        /// (i.e. <see cref="cCapabilities.CondStore"/> is in use and the mailbox supports the persistent storage of mod-sequences.)
        /// If a message has been modified since the specified value then the server will fail the store for that message.
        /// </remarks>
        public cStoreFeedback Store(IEnumerable<cMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 3);
            var lFeedback = new cStoreFeedback(pMessages, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        internal async Task<cStoreFeedback> StoreAsync(iMessageHandle pMessageHandle, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 1);
            var lFeedback = new cStoreFeedback(pMessageHandle, pOperation, pFlags);
            await ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        internal async Task<cStoreFeedback> StoreAsync(IEnumerable<iMessageHandle> pMessageHandles, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 2);
            var lFeedback = new cStoreFeedback(pMessageHandles, pOperation, pFlags);
            await ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        /// <summary>
        /// Asynchronously stores flags for a set of messages. The mailbox that the messages are in must be selected.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="Store(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public async Task<cStoreFeedback> StoreAsync(IEnumerable<cMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 3);
            var lFeedback = new cStoreFeedback(pMessages, pOperation, pFlags);
            await ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        private async Task ZStoreAsync(cStoreFeedback pFeedback, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZStoreAsync), pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate);

            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

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