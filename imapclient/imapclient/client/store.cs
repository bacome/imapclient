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
        internal cStoreFeedback Store(iMessageHandle pHandle, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 1);
            var lFeedback = new cStoreFeedback(pHandle, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        internal cStoreFeedback Store(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 2);
            var lFeedback = new cStoreFeedback(pHandles, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        /// <summary>
        /// <para>Store flags for a set of messages.</para>
        /// <para>The mailbox that the messages are in must be selected.</para>
        /// </summary>
        /// <param name="pMessages">The set of messages.</param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags">The flags to store.</param>
        /// <param name="pIfUnchangedSinceModSeq">
        /// <para>The modseq to use in the unchangedsince clause of a conditional store (RFC 7162).</para>
        /// <para>Can only be specified if the mailbox supports RFC 7162.</para>
        /// <para>If the message has been modified since the specified modseq the server should fail the update.</para>
        /// </param>
        /// <returns>Feedback on the success (or otherwise) of the store.</returns>
        public cStoreFeedback Store(IEnumerable<cMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 3);
            var lFeedback = new cStoreFeedback(pMessages, pOperation, pFlags);
            var lTask = ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        internal async Task<cStoreFeedback> StoreAsync(iMessageHandle pHandle, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 1);
            var lFeedback = new cStoreFeedback(pHandle, pOperation, pFlags);
            await ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        internal async Task<cStoreFeedback> StoreAsync(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 2);
            var lFeedback = new cStoreFeedback(pHandles, pOperation, pFlags);
            await ZStoreAsync(lFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        /**<summary>The async version of <see cref="Store(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>.</summary>*/
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