using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCache : cPersistentCacheComponent, IDisposable
    {
        private void ZMaintenance(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZMaintenance));

            // take a copy of the collections so they can be updated at the end
            //  (and these are the ones passed to the sub-class's maintenance)


            // processing

            ;?; // is this really required? 
            foreach (var lPair in mPendingItems)
            {
                var lItem = lPair.Key;
                if (!lItem.Deleted && !lItem.ToBeDeleted && lItem.SectionId != null) if (ZMessageIsToBeDeleted(lItem.SectionId.MessageUID)) lItem.Delete(lContext);
            }

            ;?; // NOTE: physically delete from the collections: expunged handles, expired handles, deleted UIDs, old UIDValidities and null valued entries (they aren't in any danger of coming back to life)

            var lSectionHandlesToRemove = new List<cSectionHandle>();

            foreach (var lPair in mSectionHandleToItem)
            {
                var lSectionHandle = lPair.Key;
                var lMessageHandle = lSectionHandle.MessageHandle;
                var lItem = lPair.Value;

                if (lItem == null) // null entries are the result of renames
                {
                    lSectionHandlesToRemove.Add(lSectionHandle);
                    continue;
                }

                if (lMessageHandle.Expunged)
                {
                    lItem.Delete(lContext);
                    lSectionHandlesToRemove.Add(lSectionHandle);
                    continue;
                }



                var lSectionId = lItem.SectionId;

                ;?; // indexed should be a triple including cannot index

                if (!lItem.Indexed && !lItem.Deleted && !lItem.ToBeDeleted && lSectionId != null)
                {
                    if (!ZMessageIsToBeDeleted(lSectionId.MessageUID))
                    {
                        if (mSectionIdToItem.TryGetValue(lSectionId, out var lExistingItem))
                        {
                            if (lExistingItem != null && !lExistingItem.TryTouch(lContext) && mSectionIdToItem.TryUpdate(lSectionId, lItem, lExistingItem)) lItem.SetIndexed(lContext);
                        }
                        else if (mSectionIdToItem.TryAdd(lSectionId, lItem)) lItem.SetIndexed(lContext);
                    }

                    if (!lItem.Indexed) lItem.Delete(lContext);
                }





                ;?; // is disposing => pfinal
                if (mDisposing || !ReferenceEquals(lItem.SectionHandle.Client.SelectedMailboxDetails?.MessageCache, lMessageHandle.MessageCache))
                {
                    if (lMessageHandle.UID == null) lItem.Delete(lContext); // if we don't have a UID then no point to keep the cached data
                    lSectionHandlesToRemove.Add(lSectionHandle); // we will never need the entry in the dictionary again as the message handle is invalid
                    continue;
                }
            }

            foreach (var lSectionHandle in lSectionHandlesToRemove) mSectionHandleToItem.TryRemove(lSectionHandle, out _);

            // collect duplicate items for deletion

            ;?;

            // delete

            ;?;

            // index items

            ;?;

            // trypersist

            ;?;


            // TODO: internalerror numbers check
            foreach (var lMessageUID in lExpungedMessages) if (!mExpungedMessages.TryRemove(lMessageUID, out _)) throw new cInternalErrorException(lContext, 1);
            foreach (var lPair in lMailboxIdToUIDValidity) mMailboxIdToUIDValidity.TryUpdate(lPair.Key, -2, lPair.Value);










            ;?; // build the maintenance info that we are going to use this time from the synchronised queues

            // delete duplicates and invalids, index items that can be indexed

            foreach (var lPair in mNonPersistentKeyItems)
            {
                var lNPKItem = lPair.Value;

                if (lNPKItem.Deleted || lNPKItem.ToBeDeleted || lNPKItem.Indexed) continue;

                ;?; // if it is expunged, try delete
                ;?; // if there is no UID and the message cache has changed, trydelete
                ;?; // if there is no UID continue [note that the API GetPerstentkey should be changed to SET persistent key and should only be allowed on npk items]
                ;?; // check for uidvalidity change: trydelete


                if ()

                    if (!lNPKItem.IsValidToCache)
                    {
                        ;?; // try delete
                        lNPKItem.SetIndexed(lContext);
                        continue;
                    }

                ;?;

                if (!lNPKItem.IsValidToCache || lNPKItem.Indexed) continue;

                ;?; // check if it is on the deleted list and delete
                ;?; // check if the UIDvalidity is wrong and dekete


                if (lNPKItem.GetPersistentKey() == null)
                {
                    if (mDisposing || !lPair.Key.IsValidToCache)
                    {
                        lNPKItem.TryDelete(-2, lContext);
                        if (pCancellationToken.IsCancellationRequested) return;
                    }

                    continue;
                }

                if (mPersistentKeyItems.TryGetValue(lNPKItem.GetPersistentKey(), out var lPKItem))
                {
                    ;?; // equals
                    if (lPKItem.ItemId == lNPKItem.ItemId) lNPKItem.SetIndexed(lContext);
                    else
                    {
                        if (lPKItem.TryTouch(lContext)) lNPKItem.TryDelete(-2, lContext);
                        else if (mPersistentKeyItems.TryUpdate(lNPKItem.GetPersistentKey(), lNPKItem, lPKItem)) lNPKItem.SetIndexed(lContext);
                        if (pCancellationToken.IsCancellationRequested) return;
                    }
                }
                else if (mPersistentKeyItems.TryAdd(lNPKItem.GetPersistentKey(), lNPKItem)) lNPKItem.SetIndexed(lContext);
            }

            if (pCancellationToken.IsCancellationRequested) return;

            // assign pks

            foreach (var lPair in mPersistentKeyItems)
            {
                var lPKItem = lPair.Value;

                if (!lPKItem.IsValidToCache) continue;

                ;?; // check if it is on the deleted list and delete
                ;?; // check if the UIDvalidity is wrong and dekete

                if (lPKItem.PersistentKeyAssigned) continue;

                lPKItem.TryAssignPersistentKey(lContext);
                if (!lPKItem.PersistentKeyAssigned && mDisposing) lPKItem.TryDelete(-2, lContext);
                if (pCancellationToken.IsCancellationRequested) return;
            }

            if (pCancellationToken.IsCancellationRequested) return;

            // cache specific maintenance

            Maintenance(pCancellationToken, lContext);
        }
    }
}