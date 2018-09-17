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

                var lResult = await lSession.SelectExamineAsync(lMC, pMailboxHandle, pForUpdate, lContext).ConfigureAwait(false);

                ;?; // note that the result includes whether qresync was requested or not
                ;?; // note that the result should include the callback for turning on the sethighestmodseq IF it needs turning on

                if (lResult.UIDNotSticky || lResult.UIDValidity == 0) lSession.PersistentCache.ClearCachedItems(pMailboxHandle.MailboxId, lContext);
                else
                {
                    // get the list of UIDs from the cache
                    //  knock out the ones that are in the result (this is the set that qresync brought into sync)
                    //   if there are any left, then manually sync those

                    // (as these are special cases, there should be a session routine to do them I guess

                    // manually synch those:
                    //
                    //  if qresync is on
                    //
                    //   s100 UID FETCH 300:500 (FLAGS) (CHANGEDSINCE 12345 VANISHED)
                    //
                    //
                    //  if qresync is off
                    //
                    //   manully sync for delete
                    //   if condsotre is on, manulyl synch flags for changedsince
                    //   else re-retrieve all the flags in the flag cache
                    //


                    // THEN: turn on telling the cache about highest mod seq










                    // if the mailbox supports modseq (i.e. highestmodseq is not zero)
                    //  get the list of UIDs from the flag cache and the highestmodeseq rom the flag cache
                    //   knock out the ones that are in the result
                    //    if there are any left, then manually sync 


                    ;?; //
                    // if the result contains a highestmodseq (from the cache
                    //
                    // get the list of UIDs from the flag cache
                    //  knock out the ones that are in the result
                }

                { // first thoughts
                    if (lResult.ManuallySynchroniseExpunged)
                    {
                        var lUIDsInCache = lSession.PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lResult.UIDValidity, lContext);

                        if (lUIDsInCache.Count > 0)
                        {
                            var lUIDsInMailbox = await ZGetUIDsAsync(lMC, pMailboxHandle, new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lUIDsInCache select lUID.UID, 50)), cSort.None, null, lContext);
                            lUIDsInCache.ExceptWith(lUIDsInMailbox);
                            lSession.PersistentCache.MessagesExpunged(pMailboxHandle.MailboxId, lUIDsInCache, lContext);
                        }
                    }

                    if (lResult.manuallysynchroniseflags)
                    {
                        // get the uids from the flag cache only : if none don't do it
                        // changedsince query is required: UID FETCH 1:*(FLAGS)(CHANGEDSINCE xxx)

                        // turn on highestmodseq tracking to the cache
                        lResult.setcallsethighestmodseq(lContext);
                    }
                }




                xxelse if (!lUsedQResync || lResult.HighestModSeq == 0) // if I didn't try using QResync OR the server doesn't store modseqs for this mailbox
                {
                    ;?; // if no modseq, don't include the flag cache

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
                    }
                }
            }
        }
    }
}