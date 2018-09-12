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
                //   pass this pair to select/examine along with the set of uids in cache (this minimises the number of fetches sent)
                //   (note that if we dont have parameters (either the UIDVal or highestmodseq is zero), then don't use qresync)
                //   (note that is there is nothing in cache, then don't use qresync)
                //
                //   (if qressync is on and gets used then by the time the select returns the persistent cache is synchronised)

                bool lUsedQResync = false;
                cSelectResult lResult;

                if (pForUpdate) lResult = await lSession.SelectAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);
                else lResult = await lSession.ExamineAsync(lMC, pMailboxHandle, lContext).ConfigureAwait(false);

                ;?; // note that the result should include the callback for turning on the sethighestmodseq

                if (lResult.UIDNotSticky || lResult.UIDValidity == 0) lSession.PersistentCache.ClearCachedItems(pMailboxHandle.MailboxId, lContext);
                else if (!lUsedQResync || lResult.HighestModSeq == 0) // if I didn't try using QResync OR the server doesn't store modseqs for this mailbox
                { 
                    var lUIDsInCache = lSession.PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lResult.UIDValidity, lContext);
                    if (lUIDsInCache.Count == 0) return;

                    // synchronise the deletes that have occurred while we haven't been watching
                    //
                    var lUIDsInMailbox = await ZGetUIDsAsync(lMC, pMailboxHandle, new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lUIDsInCache select lUID.UID, 50)), cSort.None, null, lContext);
                    lUIDsInCache.ExceptWith(lUIDsInMailbox);
                    lSession.PersistentCache.MessagesExpunged(pMailboxHandle.MailboxId, lUIDsInCache, lContext);

                    // synchronise the flag changes that have occurred while we haven't been watching
                    //  (only if condstore is on for this mailbox [otherwise we can't cache flags because to update them we have to get them all => no point to the cache])
                    //
                    if (lResult.HighestModSeq != 0)
                    {
                        // changedsince query is required: UID FETCH 1:*(FLAGS)(CHANGEDSINCE xxx)

                        // turn on highestmodseq tracking to the cache
                        lResult.setcallsethighestmodseq(lContext);
                    }
                }
            }
        }
    }
}