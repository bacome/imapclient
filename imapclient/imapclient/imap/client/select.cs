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

                // get the parameters for qresync [if it is in use]
                //  (this code in cacheintegration)
                //   ask each cache for its uidvalidity/highestmodseq for the mailbox
                //   choose the one with the highest uidvalidiy and within uidval, the lowest highestmodseq
                //   pass this pair to select/examine
                // [note that to process the vanished responses the session needs a delegate to messagesexpunged]

                cSelectResult lResult;

                if (pForUpdate) lResult = await lSession.SelectAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                else lResult = await lSession.ExamineAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);

                if (lResult.UIDNotSticky || lResult.UIDValidity == 0) ZCacheIntegrationSetMailboxUIDValidity(pMailboxHandle.MailboxId, -1, lContext);
                else
                {
                    // only if qresync isn't in use
                    var lUIDsInCache = ZCacheIntegrationGetUIDs(pMailboxHandle.MailboxId, lResult.UIDValidity, lContext);
                    if (lUIDsInCache.Count == 0) return;
                    var lUIDsInMailbox = await ZGetUIDsAsync(lMC, pMailboxHandle, new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lUIDsInCache select lUID.UID, 50)), cSort.None, null, lContext);
                    lUIDsInCache.ExceptWith(lUIDsInMailbox);
                    ZCacheIntegrationMessagesExpunged(pMailboxHandle.MailboxId, lUIDsInCache, lContext);

                    // note: for the flag cache if qresync isn't in use but condstore is, then a changedsince query is required: UID FETCH 1:* (FLAGS) (CHANGEDSINCE xxx)
                    // note that if neither condstore nor qresync is in use then the flagcache CANNOT be used (there is no point)
                }

                // now pass the lookups (uid -> headers, uid -> flags) to the session [using the mailboxhandle]
                // [on de-select the (header and flag) caches need to get the uidvalidity, highestmodseq and all messagehandles with a uid so they can merge with their existing state and save]
                // but also note that if neither condstore nor qresync is in use then the flag cache must not be updated
            }
        }
    }
}