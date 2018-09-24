using System;
using System.Collections.Generic;
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

                if (lResult.UIDValidity == 0 || lResult.UIDNotSticky) lSession.PersistentCache.ClearCachedItems(pMailboxHandle.MailboxId, lContext);
                else
                {
                    var lMailboxUID = new cMailboxUID(pMailboxHandle.MailboxId, lResult.UIDValidity);
                    var lUIDsToResync = lSession.PersistentCache.GetUIDs(lMailboxUID, lContext);

                    if (lResult.QResyncedUIDs != null) lUIDsToResync.ExceptWith(lResult.QResyncedUIDs);
                    
                    if (lUIDsToResync.Count > 0)
                    {
                        if ((lSession.EnabledExtensions & fEnableableExtensions.qresync) != 0 && lResult.CachedHighestModSeq != 0)
                        {
                            // uid fetch <luids> (FLAGS) (CHANGEDSINCE <cachedhighestmodseq> VANISHED)
                            await lSession.FetchResyncAsync(lMC, pMailboxHandle, lUIDsToResync, lResult.CachedHighestModSeq, true, lContext).ConfigureAwait(false);
                        }
                        else
                        {
                            // manually resync deleted items

                            IEnumerable<cUID> lUIDsThatExist;

                            cFilter lFilter = new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lUIDsToResync select lUID.UID, 50));

                            if (lSession.Capabilities.ESearch) lUIDsThatExist = await lSession.UIDSearchExtendedAsync(lMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);
                            else lUIDsThatExist = await lSession.UIDSearchAsync(lMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);

                            var lVanished = new List<cUID>(lUIDsToResync.Except(lUIDsThatExist));

                            PersistentCache.MessagesExpunged(pMailboxHandle.MailboxId, lVanished, lContext);

                            // manually resync the flags for any UIDs left

                            lUIDsToResync.ExceptWith(lVanished);

                            if (lUIDsToResync.Count > 0)
                            {
                                ;?; // this is the same API just with different parameners
                                if (lSession.Capabilities.CondStore && lResult.CachedHighestModSeq != 0)
                                {
                                    // uid fetch <luids> (FLAGS) (CHANGEDSINCE <cachedhighestmodseq>)
                                    await lSession.FetchResyncAsync(lMC, pMailboxHandle, lUIDsToResync, lResult.CachedHighestModSeq, false, lContext).ConfigureAwait(false); 
                                }
                                else
                                {
                                    // uid fetch <uids> (FLAGS)
                                    await lSession.UIDFetchCacheItemsAsync(lMC, pMailboxHandle, new cUIDList(lUIDsToResync), cMessageCacheItems.Flags, null, lContext).ConfigureAwait(false);
                                }
                            }
                        }
                    }

                    // after we are sure that the cache is in sync we can start telling the cache about the highestmodseq
                    lResult.SetCallSetHighestModSeq(lContext);


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