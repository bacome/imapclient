using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task UIDStoreAsync(iMailboxHandle pMailboxHandle, cUIDStoreFeedback pFeedback, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(UIDStoreAsync), pFeedback, pIfUnchangedSinceModSeq);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.SelectedMailboxDetails?.SelectedForUpdate != true) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

            if (pFeedback.Items.Count == 0) return;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                await lSession.UIDStoreAsync(lMC, pMailboxHandle, pFeedback, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}