using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
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
        /// (i.e. <see cref="cIMAPCapabilities.CondStore"/> is in use and the mailbox supports the persistent storage of mod-sequences.)
        /// If a message has been modified since the specified value then the server will fail the store for that message.
        /// </remarks>
        public cStoreFeedback Store(IEnumerable<cIMAPMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = RootContext.NewMethodV(nameof(cIMAPClient), nameof(Store), 3);
            var lFeedback = new cStoreFeedback(pMessages, pOperation, pFlags);
            var lTask = StoreAsync(lFeedback, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lFeedback;
        }

        /// <summary>
        /// Asynchronously stores flags for a set of messages. The mailbox that the messages are in must be selected.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <inheritdoc cref="Store(IEnumerable{cIMAPMessage}, eStoreOperation, cStorableFlags, ulong?)" select="returns|remarks"/>
        public async Task<cStoreFeedback> StoreAsync(IEnumerable<cIMAPMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = RootContext.NewMethodV(nameof(cIMAPClient), nameof(StoreAsync), 3);
            var lFeedback = new cStoreFeedback(pMessages, pOperation, pFlags);
            await StoreAsync(lFeedback, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lFeedback;
        }

        internal async Task StoreAsync(cStoreFeedback pFeedback, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(StoreAsync), pFeedback, pIfUnchangedSinceModSeq);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate);

            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

            if (pFeedback.Items.Count == 0) return;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                await lSession.StoreAsync(lMC, pFeedback, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}