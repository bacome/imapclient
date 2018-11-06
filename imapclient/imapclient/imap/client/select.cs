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
        ;?; // should have progress for long cache synch
        ;?; //  this now could be a long running command => separate cancellation and timeout are required
        ;?; //  increment should be called for each fetch returned in qresync? (some way of minimising the calls should be considered)
        ;?; //  set max used for manualy sync/ separate increment
        ;?; 

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

                // here: get the uidvalidity of the selected mailbox
                //  if it is different to the one that the cache had, clear the list of qresync'd uids; pretend qresync wasn't used











                ;?; // must consider a uidvalidity change during the select => 
                ;?; //  or just after the select, followed by some additions to the cache

                if (lResult.UIDValidity == 0) return;

                HashSet<cUID> lUIDsToResync;

                lUIDsToResync = lSession.PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lResult.UIDValidity, false, lContext);
                if (lResult.QResyncedUIDs != null) lUIDsToResync.ExceptWith(lResult.QResyncedUIDs);

                if (lUIDsToResync.Count > 0)
                {
                    ;?; // oh!; now the fetchsizer isn't in the session object: we have to pass it each time
                    if ((lSession.EnabledExtensions & fEnableableExtensions.qresync) != 0 && lResult.CachedHighestModSeq != 0) await lSession.UIDFetchCacheItemsAsync(lMC, pMailboxHandle, mSynchroniseCacheConfiguration, new cUIDList(lUIDsToResync), cMessageCacheItems.ModSeqFlags, lResult.CachedHighestModSeq, true, , lContext).ConfigureAwait(false);
                    else
                    {
                        // manually resync deleted items

                        IEnumerable<cUID> lUIDsThatExist;

                        cFilter lFilter = new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lUIDsToResync select lUID.UID, mMaxItemsInSequenceSet));

                        if (lSession.Capabilities.ESearch) lUIDsThatExist = await lSession.UIDSearchExtendedAsync(lMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);
                        else lUIDsThatExist = await lSession.UIDSearchAsync(lMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);

                        var lVanished = new List<cUID>(lUIDsToResync.Except(lUIDsThatExist));

                        PersistentCache.MessagesExpunged(pMailboxHandle.MailboxId, lVanished, lContext);

                        // manually resync the flags for any UIDs left

                        lUIDsToResync = lSession.PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lResult.UIDValidity, true, lContext);
                        if (lResult.QResyncedUIDs != null) lUIDsToResync.ExceptWith(lResult.QResyncedUIDs);

                        if (lUIDsToResync.Count > 0)
                        {

                        }



                        ;?; // the list of uids should come from the flag cache only

                        lUIDsToResync.ExceptWith(lVanished); ;?; // then this isn't required as the expunge should have already been processed

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
            }
        }
    }
}