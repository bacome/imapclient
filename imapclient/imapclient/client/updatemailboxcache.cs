using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        ;?; // rename these to flags
        public void UpdateMailboxCache(cMailboxId pMailboxId, fMailboxListProperties pProperties, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UpdateMailboxCache));
            var lTask = ZUpdateMailboxCacheAsync(pMailboxId, pFlagSets, pStatus, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
        }

        public Task UpdateMailboxCacheAsync(cMailboxId pMailboxId, fMailboxFlagSets pFlagSets, bool pStatus)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UpdateMailboxCacheAsync));
            return ZUpdateMailboxCacheAsync(pMailboxId, pFlagSets, pStatus, lContext);
        }

        private async Task ZUpdateMailboxCacheAsync(cMailboxId pMailboxId, fMailboxFlagSets pFlagSets, bool pStatus, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUpdateMailboxCacheAsync), pMailboxId, pFlagSets, pStatus);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new cAccountNotConnectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            if (lSession.ConnectedAccountId != pMailboxId.AccountId) throw new cAccountNotConnectedException(lContext);

            string lListMailbox = pMailboxId.MailboxName.Name.Replace('*', '%');
            cMailboxNamePattern lPattern = new cMailboxNamePattern(pMailboxId.MailboxName.Name, string.Empty, null);

            await ZZMailboxesAsync(lSession, lListMailbox, pMailboxId.MailboxName.Delimiter, lPattern, fMailboxTypes.all, pFlagSets, pStatus, lContext).ConfigureAwait(false);
        }
    }
}