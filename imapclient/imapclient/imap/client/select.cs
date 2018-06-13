using System;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal async Task SelectAsync(iMailboxHandle pMailboxHandle, bool pForUpdate, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(SelectAsync), pMailboxHandle, pForUpdate);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            using (var lToken = CancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(Timeout, lToken.CancellationToken);

                cSelectResult lResult;

                if (pForUpdate) lResult = await lSession.SelectAsync(lMC, pMailboxHandle, HeaderCache, lContext).ConfigureAwait(false);
                else lResult = await lSession.ExamineAsync(lMC, pMailboxHandle, HeaderCache, lContext).ConfigureAwait(false);

                if (lResult.UIDNotSticky || lResult.UIDValidity == 0) ZCacheIntegrationSetMailboxUIDValidity(pMailboxHandle.MailboxId, -1, lContext);
                else
                {
                    var lUIDsInCache = ZCacheIntegrationGetUIDs(pMailboxHandle.MailboxId, lResult.UIDValidity, lContext);
                    if (lUIDsInCache.Count == 0) return;
                    var lUIDsInMailbox = await ZGetUIDsAsync(lMC, pMailboxHandle, new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lUIDsInCache select lUID.UID, 50)), cSort.None, null, lContext);
                    lUIDsInCache.ExceptWith(lUIDsInMailbox);
                    ZCacheIntegrationMessagesExpunged(pMailboxHandle.MailboxId, lUIDsInCache, lContext);
                }
            }
        }
    }
}