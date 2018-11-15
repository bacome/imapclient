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

        private async Task ZSelectAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, bool pForUpdate, Action<long> pQResyncSetMaximum, Action<int> pQResyncIncrement, Action<long> pSynchroniseSetMaximum, Action<int> pSynchroniseIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSelectAsync), pMC, pMailboxHandle, pForUpdate);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            cQResyncParameters lQResyncParameters = null;

            if ((lSession.EnabledExtensions & fEnableableExtensions.qresync) != 0)
            {
                var lUIDValidity = PersistentCache.GetUIDValidity(pMailboxHandle.MailboxId, lContext);

                if (lUIDValidity != 0)
                {
                    var lHighestModSeq = PersistentCache.GetHighestModSeq(pMailboxHandle.MailboxId, lUIDValidity, lContext);

                    if (lHighestModSeq != 0)
                    {
                        var lUIDs = PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lUIDValidity, false, lContext);

                        if (lUIDs.Count != 0)
                        {
                            x=cSequenceSet.FromUInts(from lUID in lUIDs select lUID.UID, mMaxItemsInSequenceSet);
                            mSynchroniser.InvokeActionLong(pQResyncSetMaximum, cSASLXOAuth2.includedcount, lContext);
                            lQResyncParameters = new cQResyncParameters(lUIDValidity, lHighestModSeq, cSequenceSet.FromUInts(from lUID in lUIDs select lUID.UID, mMaxItemsInSequenceSet), pQResyncIncrement);
                        }
                    }
                }
            }

            var lSelectedMailboxCache = await lSession.SelectExamineAsync(pMC, pMailboxHandle, pForUpdate, lQResyncParameters, lContext).ConfigureAwait(false);

            if (lSelectedMailboxCache.UIDValidity == 0 || lSelectedMailboxCache.NoModSeq) return;

            if (lQResyncParameters != null && lSelectedMailboxCache.UIDValidity != lQResyncParameters.UIDValidity) lQResyncParameters = null; // didn't qresync

            // synchronise the persistent cache
            await ZSelectSynchronisePersistentCacheAsync(pMC, lSession, lSelectedMailboxCache, lQResyncParameters, pResyncSetMaximum, pResyncIncrement, lContext).ConfigureAwait(false);

            // we can start telling the cache about the highestmodseq we have
            lSelectedMailboxCache.SetSynchronised(lContext);
        }

        private async Task ZSelectSynchronisePersistentCacheAsync(cMethodControl pMC, cSession pSession, iSelectedMailboxCache pSelectedMailboxCache, cQResyncParameters pQResyncParameters, Action<long> pSetMaximum, Action<int> pIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZSelectSynchronisePersistentCacheAsync), pMC, pSelectedMailboxCache, pQResyncParameters);

            ulong lChangedSince;

            var lAllCachedUIDs = PersistentCache.GetUIDs(pSelectedMailboxCache.MailboxHandle.MailboxId, pSelectedMailboxCache.UIDValidity, false, lContext);
            if (lAllCachedUIDs.Count == 0) return; // nothing to synchronise

            if (pQResyncParameters != null)
            {
                // check if anything was added to the cache between getting the parameters for qresync and now
                //////////////////////////////////////////////////////////////////////////////////////////////

                var lAddedUIDs = new cUIDList(from lUID in lAllCachedUIDs where !pQResyncParameters.UIDs.Includes(lUID.UID, 0) select lUID);

                if (lAddedUIDs.Count == 0) return; // none - the persistent cache is in sync

                // because we passed a set of UIDs to qresync, we may not have synchronised the added UIDs
                //  we can use the qresync extension to UID FETCH to synchronise those ones now

                lChangedSince = PersistentCache.GetHighestModSeq(pSelectedMailboxCache.MailboxHandle.MailboxId, pSelectedMailboxCache.UIDValidity, lContext);

                if (lChangedSince != 0) // could be zero if there was a nomodseqflagupdate (or the UIDValidity changed)
                {
                    mSynchroniser.InvokeActionLong(pSetMaximum, lAddedUIDs.Count, lContext); // note that the maximum most likely will not be reached as we will only receive flags for the changed messages
                    await pSession.UIDFetchCacheItemsAsync(pMC, mSynchroniseCacheSizer, pSelectedMailboxCache.MailboxHandle, lAddedUIDs, cMessageCacheItems.ModSeqFlags, lChangedSince, true, pIncrement, lContext).ConfigureAwait(false);
                    return;
                }
            }

            // this is the case where we have not done a qresync (or the cache got trashed by a nomodseqflagupdate)
            ///////////////////////////////////////////////////////////////////////////////////////////////////////

            if ((pSession.EnabledExtensions & fEnableableExtensions.qresync) != 0)
            {
                // we can use the qresync extension to UID FETCH to synchronise

                lChangedSince = PersistentCache.GetHighestModSeq(pSelectedMailboxCache.MailboxHandle.MailboxId, pSelectedMailboxCache.UIDValidity, lContext);

                if (lChangedSince != 0)
                {
                    var lUIDsToSynchronise = new cUIDList(lAllCachedUIDs);
                    mSynchroniser.InvokeActionLong(pSetMaximum, lUIDsToSynchronise.Count, lContext); // note that the maximum most likely will not be reached as we will only receive flags for the changed messages
                    await pSession.UIDFetchCacheItemsAsync(pMC, mSynchroniseCacheSizer, pSelectedMailboxCache.MailboxHandle, lUIDsToSynchronise, cMessageCacheItems.ModSeqFlags, lChangedSince, true, pIncrement, lContext).ConfigureAwait(false);
                    return;
                }
            }

            // get this now in case things are added while we are processing (if they are being added then I should get flag updates and expunges) 
            var lCachedUIDsWithModSeqFlags = PersistentCache.GetUIDs(pSelectedMailboxCache.MailboxHandle.MailboxId, pSelectedMailboxCache.UIDValidity, true, lContext);

            // reconcile the list of UIDs that are in the persistent cache with the UIDs that are on the server to work out which ones have been deleted

            cFilter lFilter = new cFilterUIDIn(pSelectedMailboxCache.UIDValidity, cSequenceSet.FromUInts(from lUID in lAllCachedUIDs select lUID.UID, mMaxItemsInSequenceSet));

            IEnumerable<cUID> lUIDsTheServerStillHas;
            if (pSession.Capabilities.ESearch) lUIDsTheServerStillHas = await pSession.UIDSearchExtendedAsync(pMC, pSelectedMailboxCache.MailboxHandle, lFilter, lContext).ConfigureAwait(false);
            else lUIDsTheServerStillHas = await pSession.UIDSearchAsync(pMC, pSelectedMailboxCache.MailboxHandle, lFilter, lContext).ConfigureAwait(false);

            var lUIDsThatHaveVanished = new List<cUID>(lAllCachedUIDs.Except(lUIDsTheServerStillHas));

            if (lUIDsThatHaveVanished.Count > 0)
            {
                PersistentCache.MessagesExpunged(pSelectedMailboxCache.MailboxHandle.MailboxId, lUIDsThatHaveVanished, lContext);
                lCachedUIDsWithModSeqFlags.ExceptWith(lUIDsThatHaveVanished);
            }

            // re-get the flags that are in the persistent cache

            if (lCachedUIDsWithModSeqFlags.Count == 0) return; // none to get

            var lUIDsToGetFlagsFor = new cUIDList(lCachedUIDsWithModSeqFlags);

            if (pSession.Capabilities.CondStore) lChangedSince = PersistentCache.GetHighestModSeq(pSelectedMailboxCache.MailboxHandle.MailboxId, pSelectedMailboxCache.UIDValidity, lContext);
            else lChangedSince = 0;

            mSynchroniser.InvokeActionLong(pSetMaximum, lUIDsToGetFlagsFor.Count, lContext);
            await pSession.UIDFetchCacheItemsAsync(pMC, mSynchroniseCacheSizer, pSelectedMailboxCache.MailboxHandle, lUIDsToGetFlagsFor, cMessageCacheItems.ModSeqFlags, lChangedSince, false, pIncrement, lContext).ConfigureAwait(false);
        }
    }
}
