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

        internal async Task SelectAsync(iMailboxHandle pMailboxHandle, bool pForUpdate, cMethodConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(SelectAsync), pMailboxHandle, pForUpdate, pConfiguration);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            if (pConfiguration == null)
            {
                using (var lToken = CancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    await ZSelectAsync(lMC, pMailboxHandle, pForUpdate, null, null, null, lContext).ConfigureAwait(false);
                }
            }
            else await ZSelectAsync(pConfiguration.MC, pMailboxHandle, pForUpdate, pConfiguration.Increment1, pConfiguration.SetMaximum2, pConfiguration.Increment2, lContext).ConfigureAwait(false);
        }

        private async Task ZSelectAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, bool pForUpdate, Action<int> pQResyncIncrement, Action<long> pResyncSetMaximum, Action<int> pResyncIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSelectAsync), pMC, pMailboxHandle, pForUpdate);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            cQResyncParameters lQResyncParameters;

            var lCachedUIDValidity = PersistentCache.GetUIDValidity(pMailboxHandle.MailboxId, lContext);

            if (lCachedUIDValidity == 0) lQResyncParameters = null;
            else
            {
                var lCachedHighestModSeq = PersistentCache.GetHighestModSeq(pMailboxHandle.MailboxId, lCachedUIDValidity, lContext);

                if (lCachedHighestModSeq == 0) lQResyncParameters = null;
                else
                {
                    var lCachedUIDs = PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lCachedUIDValidity, false, lContext);
                    if (lCachedUIDs.Count == 0) lQResyncParameters = null;
                    else lQResyncParameters = new cQResyncParameters(lCachedUIDValidity, lCachedHighestModSeq, lCachedUIDs, mMaxItemsInSequenceSet, pQResyncIncrement);
                }
            }

            var lSelectedMailboxCache = await lSession.SelectExamineAsync(pMC, pMailboxHandle, pForUpdate, lQResyncParameters, lContext).ConfigureAwait(false);

            if (lSelectedMailboxCache.UIDValidity == 0 || lSelectedMailboxCache.NoModSeq) return;

            if (lSelectedMailboxCache.UIDValidity != lCachedUIDValidity) lQResyncParameters = null; // didn't qresync

            // synchronise the cache
            await ZCacheResyncAsync(pMC, pMailboxHandle, lResult,  ).configureawait(false);

            // after we are sure that the cache is in sync we can start telling the cache about the highestmodseq
            lResult.SetSynchronised(lContext);
        }

        private async Task ZCacheResyncAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, uint pUIDValidity, cUIDList pQResyncedUIDs, Action<long> pResyncSetMaximum, Action<int> pResyncIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCacheResyncAsync), pMC, pMailboxHandle, pUIDValidity, pQResyncedUIDs, ...);

            var lUIDsToResync = PersistentCache.GetUIDs(pMailboxHandle.MailboxId, pUIDValidity, false, lContext);




            if (lQResyncUIDs != null) lResyncUIDs.ExceptWith(lQResyncUIDs); // remove the ones that have already been resynchronised by qresync

            if (lResyncUIDs.Count > 0)
            {
                if (lQResyncUIDValidityx == 0)
                {
                    // manually reconcile UIDs between the cache and the server, removing UIDs that have vanished from the cache

                    IEnumerable<cUID> lServerUIDs;

                    cFilter lFilter = new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lResyncUIDs select lUID.UID, mMaxItemsInSequenceSet));

                    if (lSession.Capabilities.ESearch) lServerUIDs = await lSession.UIDSearchExtendedAsync(pMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);
                    else lServerUIDs = await lSession.UIDSearchAsync(pMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);

                    var lVanishedUIDs = new List<cUID>(lResyncUIDs.Except(lServerUIDs));

                    PersistentCache.MessagesExpunged(pMailboxHandle.MailboxId, lVanishedUIDs, lContext);

                    // manually resync flags in the cache

                    lResyncUIDs = PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lResult.uidvalidity, true, lContext);
                    if (lQResyncUIDValidity == lResult.uidvalidity) lResyncUIDs.ExceptWith(lQResyncUIDs);

                    if (lResyncUIDs.Count == 0)
                    {
                        ;?; // can use highestmodseq here is condsotre os non
                    }




                    ;?;
                }
                else 
                {
                    // this is the case where between the beginning of this routine and the qresync, some things were added to the cache
                    //  the problem being that because we pass the set of UIDs to qresync, that we haven't resync'd the things that were added
                    //  we can use the qresync extension to UID FETCH to resync those things

                    mSynchroniser.InvokeActionLong(pManualResyncSetMaximum, lResyncUIDs.Count, lContext);
                    await lSession.UIDFetchCacheItemsAsync(pMC, mSynchroniseCacheSizer, pMailboxHandle, lResyncUIDs, cMessageCacheItems.ModSeqFlags, lQResyncHighestModSeqx, true, pManualResyncIncrement, lContext).ConfigureAwait(false);
                }
            }

            // after we are sure that the cache is in sync we can start telling the cache about the highestmodseq
            lResult.EnableCallSetHighestModSeq(lContext);











            bool lUsingQResync;

            if (lUIDsToQResync == null || lUIDsToQResync.Count == 0)
            {
                lUsingQResync = false;
                if (_Capabilities.CondStore) lBuilder.Add(kSelectCommandPartCondStore);
            }
            else
            {
                lUsingQResync = true;
                lBuilder.Add(kSelectCommandPartQResync);
                lBuilder.Add(new cTextCommandPart(lUIDValidity));
                lBuilder.Add(cCommandPart.Space);
                lBuilder.Add(new cTextCommandPart(lCachedHighestModSeq));
                lBuilder.Add(cCommandPart.Space);
                lBuilder.Add(new cTextCommandPart(cSequenceSet.FromUInts(from lUID in lUIDsToQResync select lUID.UID, mMaxItemsInSequenceSet)));
                lBuilder.Add(kSelectCommandPartRParenRParen);
            }










            ;?; // pass config incremnt1

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