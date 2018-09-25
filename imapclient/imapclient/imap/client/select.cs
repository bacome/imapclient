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

                if (lResult.UIDValidity != 0 && !lResult.UIDNotSticky)
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

                            cFilter lFilter = new cFilterUIDIn(lResult.UIDValidity, cSequenceSet.FromUInts(from lUID in lUIDsToResync select lUID.UID, mMaxItemsInSequenceSet));

                            if (lSession.Capabilities.ESearch) lUIDsThatExist = await lSession.UIDSearchExtendedAsync(lMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);
                            else lUIDsThatExist = await lSession.UIDSearchAsync(lMC, pMailboxHandle, lFilter, lContext).ConfigureAwait(false);

                            var lVanished = new List<cUID>(lUIDsToResync.Except(lUIDsThatExist));

                            PersistentCache.MessagesExpunged(pMailboxHandle.MailboxId, lVanished, lContext);

                            // manually resync the flags for any UIDs left

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
}