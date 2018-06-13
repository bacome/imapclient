using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cSectionCache : cPersistentCache, IDisposable
    {
        private static readonly TimeSpan kPlusOneHour = TimeSpan.FromHours(1);
        private static readonly TimeSpan kMinusOneHour = TimeSpan.FromHours(-1);

        private bool mDisposed = false;
        private bool mDisposing = false;

        public readonly string InstanceName;
        public readonly int MaintenanceFrequency;

        protected readonly cTrace.cContext mRootContext;

        private readonly ConcurrentDictionary<cSectionId, cSectionCacheItem> mSectionIdToItem = new ConcurrentDictionary<cSectionId, cSectionCacheItem>();
        private readonly ConcurrentDictionary<cSectionHandle, cSectionCacheItem> mSectionHandleToItem = new ConcurrentDictionary<cSectionHandle, cSectionCacheItem>();

        // lock and collections for recording data used in maintenance to delete items
        private readonly object mExpiredLock = new object();
        private HashSet<cMessageUID> mExpungedMessages = new HashSet<cMessageUID>();
        private Dictionary<cMailboxId, long> mMailboxToUIDValidity = new Dictionary<cMailboxId, long>();

        // pending items: new items that haven't been added to the cache yet (items are in these collections from when getnewitem is called until additem is called)
        private readonly object mPendingItemsLock = new object();
        private readonly HashSet<cSectionCacheItem> mPendingSectionIdItems = new HashSet<cSectionCacheItem>();
        private readonly HashSet<cSectionCacheItem> mPendingSectionHandleItems = new HashSet<cSectionCacheItem>();

        // source for numbering cache items
        private int mItemSequence = 7;

        // maintenance background task
        private readonly object mMaintenanceStartLock = new object();
        private CancellationTokenSource mMaintenanceCTS;
        private Task mMaintenanceTask = null;

        protected cSectionCache(string pInstanceName, int pMaintenanceFrequency)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            if (MaintenanceFrequency < 1000) throw new ArgumentOutOfRangeException(nameof(pMaintenanceFrequency));
            MaintenanceFrequency = pMaintenanceFrequency;
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
        }

        ;?; // TODO: check context set, parameters validated, maintenance started

        public override HashSet<cUID> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetUIDs), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            YMaintenanceStart(lContext);

            var lUIDs = new HashSet<cUID>();

            // when moving from pending it is important to add to the dictionary before removing from pending

            lock (mPendingItemsLock)
            {
                foreach (var lItem in mPendingSectionIdItems) LAddFromUID(lItem.SectionId.MessageUID, lItem);
                foreach (var lItem in mPendingSectionHandleItems) LAddFromHandle(lItem.SectionHandle.MessageHandle, lItem);
            }

            foreach (var lPair in mSectionHandleToItem) LAddFromHandle(lPair.Key.MessageHandle, lPair.Value);
            foreach (var lPair in mSectionIdToItem) LAddFromUID(lPair.Key.MessageUID, lPair.Value);

            return lUIDs;

            void LAddFromUID(cMessageUID pMessageUID, cSectionCacheItem pItem)
            {
                if (pMessageUID.MailboxId == pMailboxId && pMessageUID.UID.UIDValidity == pUIDValidity && !pItem.Deleted && !pItem.ToBeDeleted) lUIDs.Add(pMessageUID.UID);
            }

            void LAddFromHandle(iMessageHandle pMessageHandle, cSectionCacheItem pItem)
            {
                if (!pMessageHandle.Expunged && pMessageHandle.UID != null && pMessageHandle.MessageCache.MailboxHandle.MailboxId == pMailboxId && pMessageHandle.UID.UIDValidity == pUIDValidity && !pItem.Indexed && !pItem.Deleted && !pItem.ToBeDeleted) lUIDs.Add(pMessageHandle.UID);
            }
        }

        public override void MessageExpunged(cMailboxId pMailboxId, cUID pUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(MessageExpunged), pMailboxId, pUID);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));

            YMaintenanceStart(lContext);

            var lMessageUID = new cMessageUID(pMailboxId, pUID);

            lock (mExpiredLock) { mExpungedMessages.Add(lMessageUID); }
        }

        public override void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(MessagesExpunged), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            YMaintenanceStart(lContext);

            var lMessageUIDs = new List<cMessageUID>(from lUID in pUIDs select new cMessageUID(pMailboxId, lUID));

            lock (mExpiredLock) { mExpungedMessages.UnionWith(lMessageUIDs); }
        }

        public override void SetMailboxUIDValidity(cMailboxId pMailboxId, long pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(SetMailboxUIDValidity), pMailboxId, pUIDValidity);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            YMaintenanceStart(lContext);
            lock (mExpiredLock) { mMailboxToUIDValidity[pMailboxId] = pUIDValidity; }
        }

        public sealed override void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);

            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            if (!YCanCopy) return;

            YMaintenanceStart(lContext);

            var lSectionIdToItemId = new Dictionary<cSectionId, object>();

            var lDestinationMailboxId = new cMailboxId(pSourceMailboxId.AccountId, pDestinationMailboxName);

            foreach (var lPair in mSectionHandleToItem)
            {
                var lMessageHandle = lPair.Key.MessageHandle;
                var lItem = lPair.Value;

                if (!lItem.Indexed && !lItem.Deleted && !lItem.ToBeDeleted && !lMessageHandle.Expunged && lMessageHandle.UID != null && lMessageHandle.MessageCache.MailboxHandle.MailboxId == pSourceMailboxId && pFeedback.TryGetValue(lMessageHandle.UID, out var lCreatedUID))
                {
                    var lSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lCreatedUID), lPair.Key.Section, lPair.Key.Decoding);
                    lSectionIdToItemId[lSectionId] = lItem.ItemId;
                }
            }

            foreach (var lPair in mSectionIdToItem)
            {
                var lMessageUID = lPair.Key.MessageUID;
                var lItem = lPair.Value;

                if (!lItem.CameFromCache && !lItem.Deleted && !lItem.ToBeDeleted && lMessageUID.MailboxId == pSourceMailboxId && pFeedback.TryGetValue(lMessageUID.UID, out var lCreatedUID))
                {
                    var lSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lCreatedUID), lPair.Key.Section, lPair.Key.Decoding);
                    lSectionIdToItemId[lSectionId] = lItem.ItemId;
                }
            }

            ;?; // trycatch
            YCopy(pSourceMailboxId, lDestinationMailboxId, pFeedback, lSectionIdToItemId, lContext);
        }

        protected override HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YGetMailboxNames), pAccountId);

            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));

            YMaintenanceStart(lContext);

            var lMailboxNames = new HashSet<cMailboxName>();

            // when moving from pending it is important to add to the dictionary before removing from pending

            lock (mPendingItemsLock)
            {
                foreach (var lItem in mPendingSectionIdItems) LAddFromUID(lItem.SectionId.MessageUID.MailboxId, lItem);
                foreach (var lItem in mPendingSectionHandleItems) LAddFromHandle(lItem.SectionHandle.MessageHandle, lItem);
            }

            foreach (var lPair in mSectionHandleToItem) LAddFromHandle(lPair.Key.MessageHandle, lPair.Value);
            foreach (var lPair in mSectionIdToItem) LAddFromUID(lPair.Key.MessageUID.MailboxId, lPair.Value);

            return lMailboxNames;

            void LAddFromUID(cMailboxId pMailboxId, cSectionCacheItem pItem)
            {
                if (pMailboxId.AccountId == pAccountId && !pItem.Deleted && !pItem.ToBeDeleted) lMailboxNames.Add(pMailboxId.MailboxName);
            }

            void LAddFromHandle(iMessageHandle pMessageHandle, cSectionCacheItem pItem)
            {
                if (pMessageHandle.Expunged) return;
                var lMailboxId = pMessageHandle.MessageCache.MailboxHandle.MailboxId;
                if (lMailboxId.AccountId == pAccountId && !pItem.Indexed && !pItem.Deleted && !pItem.ToBeDeleted) lMailboxNames.Add(lMailboxId.MailboxName);
            }
        }

        protected sealed override void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YRename), pMailboxId, pMailboxName);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            YMaintenanceStart(lContext);

            var lSectionIdToItemId = new Dictionary<cSectionId, object>();

            var pDestinationMailboxId = new cMailboxId(pMailboxId.AccountId, pMailboxName);

            foreach (var lPair in mSectionHandleToItem)
            {
                var lMessageHandle = lPair.Key.MessageHandle;
                var lItem = lPair.Value;

                if (!lItem.Indexed && !lItem.Deleted && !lItem.ToBeDeleted && !lMessageHandle.Expunged && lMessageHandle.UID != null && lMessageHandle.MessageCache.MailboxHandle.MailboxId == pMailboxId)
                {
                    var lSectionId = new cSectionId(new cMessageUID(pDestinationMailboxId, lMessageHandle.UID), lPair.Key.Section, lPair.Key.Decoding);
                    lSectionIdToItemId[lSectionId] = lItem.ItemId;
                }
            }

            foreach (var lPair in mSectionIdToItem)
            {
                var lMessageUID = lPair.Key.MessageUID;
                var lItem = lPair.Value;

                if (!lItem.CameFromCache && !lItem.Deleted && !lItem.ToBeDeleted && lMessageUID.MailboxId == pMailboxId)
                {
                    var lSectionId = new cSectionId(new cMessageUID(pDestinationMailboxId, lMessageUID.UID), lPair.Key.Section, lPair.Key.Decoding);
                    lSectionIdToItemId[lSectionId] = lItem.ItemId;
                }
            }

            ;?;
            YRename(pMailboxId, pDestinationMailboxId, lSectionIdToItemId, lContext);
        }


        // asks the cache to create a new item
        //
        protected abstract cSectionCacheItem YGetNewItem(cMailboxId pMailboxId, uint pUIDValidity, bool pUIDNotSticky, cTrace.cContext pParentContext);

        public bool IsDisposed => mDisposed || mDisposing;

        // asks the cache to return and item for the section if it has one, this default implementation never returns an item 
        //
        protected virtual bool YTryGetExistingItem(cSectionId pSectionId, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YTryGetExistingItem), pSectionId);
            rItem = null;
            return false;
        }

        protected virtual bool YCanCopy => false;

        // asks the cache to copy items if it can, this default implementation does nothing
        //
        protected virtual void YCopy(cMailboxId pSourceMailboxId, cMailboxId pDestinationMailboxId, cCopyFeedback pFeedback, Dictionary<cSectionId, object> pForNewItemsTheNewSectionIdToTheItemIdToCopy, cTrace.cContext pParentContext)
        {
            ;?;
        }

        // asks the cache to rename items if it can, this default implementation does nothing
        //
        protected virtual void YRename(cMailboxId pSourceMailboxId, cMailboxId pDestinationMailboxId, Dictionary<cSectionId, object> pForNewItemsTheNewSectionIdToTheItemIdToRename, cTrace.cContext pParentContext)
        {
            ;?;
        }

        // gives the cache a chance to do time consuming maintenance
        //
        protected virtual void YMaintenance(bool pFinal, cSectionCacheMaintenanceData pData, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YMaintenance), pFinal, pData);
        }

        // makes sure that the maintenance task is running
        //
        protected void YMaintenanceStart(cTrace.cContext pParentContext)
        {
            if (mMaintenanceTask != null) return;

            lock (mMaintenanceStartLock)
            {
                if (mMaintenanceTask == null)
                {
                    mMaintenanceCTS = new CancellationTokenSource();
                    mMaintenanceTask = ZMaintenanceAsync(pParentContext);
                }
            }
        }

        protected internal int GetItemSequence() => Interlocked.Increment(ref mItemSequence);

        internal bool TryGetItemLength(cSectionId pSectionId, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemLength), pSectionId);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext, 1);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            if (mSectionIdToItem.TryGetValue(pSectionId, out var lItem))
            {
                rLength = lItem.Length;
                return true;
            }

            ;?; // try/catch
            if (TryGetExistingItem(pSectionId, out var lExistingItem, lContext))
            {
                if (lExistingItem == null || !lExistingItem.Cached) throw new cUnexpectedSectionCacheActionException(lContext, 2);

                mSectionIdToItem.TryAdd(pSectionId, lExistingItem);
                rLength = lExistingItem.Length;
                return true;
            }

            rLength = -1;
            return false;
        }

        internal bool TryGetItemReader(cSectionId pSectionId, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pSectionId);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext, 1);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));

            while (true)
            {
                cSectionCacheItem lPKItem = null;

                if (mPersistentKeyItems.TryGetValue(pKey, out lPKItem)) if (lPKItem.TryGetReader(out rReader, lContext)) return true;

                ;?;// try/catch
                if (!TryGetExistingItem(pKey, out var lExistingItem, lContext))
                {
                    rReader = null;
                    return false;
                }

                if (lExistingItem == null || !lExistingItem.Cached) throw new cUnexpectedSectionCacheActionException(lContext, 2);

                if (lExistingItem.ItemId == lPKItem.ItemId)
                {
                    rReader = null;
                    return false;
                }

                if (lPKItem == null) mPersistentKeyItems.TryAdd(pKey, lExistingItem);
                else mPersistentKeyItems.TryUpdate(pKey, lExistingItem, lPKItem);
            }
        }

        internal cSectionCacheItem GetNewItem(cSectionId pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewItem), pKey);
            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            var lItem = YGetNewItem(pKey.MessageId.MailboxId, lContext);
            if (lItem == null || !lItem.CanGetReaderWriter) throw new cUnexpectedSectionCacheActionException(lContext);
            lItem.SetKey(pKey);
            lock (mPendingItemsLock) { mPendingPersistentKeyItems.Add(lItem); }
            return lItem;
        }

        internal bool TryGetItemLength(cSectionHandle pKey, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemLength), pKey);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));

            if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem))
            {
                rLength = lItem.Length;
                return true;
            }

            rLength = -1;
            return false;
        }

        internal bool TryGetItemReader(cSectionHandle pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pKey);
            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (mNonPersistentKeyItems.TryGetValue(pKey, out var lItem)) return lItem.TryGetReader(out rReader, lContext);
            rReader = null;
            return false;
        }

        internal cSectionCacheItem GetNewItem(cSectionHandle pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewItem), pKey);
            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            var lItem = YGetNewItem(pKey.MailboxId, lContext);
            if (lItem == null || !lItem.CanGetReaderWriter) throw new cUnexpectedSectionCacheActionException(lContext);
            lItem.SetKey(pKey);
            lock (mPendingItemsLock) { mPendingNonPersistentKeyItems.Add(lItem); }
            return lItem;
        }

        internal void AddItem(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(AddItem), pItem);

            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached || (pItem.PersistentKey == null && pItem.NonPersistentKey == null)) throw new ArgumentOutOfRangeException(nameof(pItem));

            if (IsDisposed) return;
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) return;

            ;?; // note that the remove has to be done: NO RETURNS!

            if (!pItem.Deleted && !pItem.ToBeDeleted) // these will be set by maintenance if it discovers that the item has been expunged or the UIDValidity has changed
            {
                if (pItem.NonPersistentKey != null)
                {
                    pItem.TrySetPersistentKey();

                    var lMessageHandle = pItem.NonPersistentKey.MessageHandle;
                    if (lMessageHandle.Expunged) return; ;?; // NONONONO

                    if (pItem.PersistentKey == null)
                    {
                        var lClient = pItem.NonPersistentKey.Client;
                        if (!ReferenceEquals(lMessageHandle.MessageCache, lClient.SelectedMailboxDetails?.MessageCache)) return;
                    }
                }

                if (pItem.PersistentKey == null)
                {
                    if (mNonPersistentKeyItems.TryGetValue(pItem.NonPersistentKey, out var lNPKItem))
                    {
                        if (!lNPKItem.TryTouch(lContext) && mNonPersistentKeyItems.TryUpdate(pItem.NonPersistentKey, pItem, lNPKItem)) pItem.SetCached(lContext);
                    }
                    else if (mNonPersistentKeyItems.TryAdd(pItem.NonPersistentKey, pItem)) pItem.SetCached(lContext);
                }
                else
                {
                    if (mPersistentKeyItems.TryGetValue(pItem.PersistentKey, out var lPKItem))
                    {
                        if (!lPKItem.TryTouch(lContext) && mPersistentKeyItems.TryUpdate(pItem.PersistentKey, pItem, lPKItem)) pItem.SetCached(lContext);
                    }
                    else if (mPersistentKeyItems.TryAdd(pItem.PersistentKey, pItem)) pItem.SetCached(lContext);
                }
            }

            ;?; // remove from the right one;?;
            lock (mPendingLock) { mPendingNewItems.Remove(pItem); }
        }


        private async Task ZMaintenanceAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZMaintenanceAsync));

            try
            {
                while (true)
                {
                    ZMaintenance(false, lContext);
                    lContext.TraceVerbose("waiting: {0}", MaintenanceFrequency);
                    await Task.Delay(MaintenanceFrequency, mMaintenanceCTS.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (!mMaintenanceCTS.IsCancellationRequested && lContext.TraceException("the background task is stopping due to an unexpected error", e)) { }
        }

        private void ZMaintenance(bool pFinal, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZMaintenance));

            // take a copy and replace the sets that tell us about expired items

            HashSet<iMessageHandle> lExpungedMessageHandles;
            Dictionary<cMailboxId, long> lUIDValiditiesDiscovered;

            lock (mExpiredLock)
            {
                lExpungedMessageHandles = mExpunged;
                mExpunged = new HashSet<iMessageHandle>();
                lUIDValiditiesDiscovered = mUIDValiditiesDiscovered;
                mUIDValiditiesDiscovered = new Dictionary<cMailboxId, uint>();
            }

            HashSet<cMessageUID> lExpungedMessageUIDs = new HashSet<cMessageUID>();
            foreach (var lMessageHandle in mExpungedMessageHandles) if (lMessageHandle.UID != null) lExpungedMessageUIDs.Add(new cMessageUID(lMessageHandle.MessageCache.MailboxHandle.MailboxId, lMessageHandle.UID));

            // check if any pending items should be canned

            HashSet<cSectionCacheItem> lPendingPersistentKeyItems;
            HashSet<cSectionCacheItem> lPendingNonPersistentKeyItems;

            lock (mPendingItemsLock)
            {
                lPendingPersistentKeyItems = new HashSet<cSectionCacheItem>(mPendingPersistentKeyItems);
                lPendingNonPersistentKeyItems = new HashSet<cSectionCacheItem>(mPendingNonPersistentKeyItems);
            }

            foreach (var lItem in lPendingPersistentKeyItems)
            {
                var lMessageId = lItem.PersistentKey.MessageId;
                if (lExpungedMessageIds.Contains(lMessageId) || (lUIDValiditiesDiscovered.TryGetValue(lMessageId.MailboxId, out var lUIDValidity) && lMessageId.UID.UIDValidity != lUIDValidity)) lItem.TryDelete(-2, lContext);
            }

            foreach (var lItem in lPendingNonPersistentKeyItems)
            {
                ;?;
                var lMessageHandle = lItem.NonPersistentKey.MessageHandle;
                if (lExpungedMessageHandles.Contains(lMessageHandle) || (lUIDValiditiesDiscovered.TryGetValue(lMessageId.MailboxId, out var lUIDValidity) && lMessageId.UID.UIDValidity != lUIDValidity)) lItem.TryDelete(-2, lContext);
            }

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

        public override string ToString() => $"{nameof(cSectionCache)}({InstanceName})";

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool pDisposing)
        {
            if (mDisposed) return;

            if (pDisposing)
            {
                mDisposing = true;

                if (mMaintenanceCTS != null && !mMaintenanceCTS.IsCancellationRequested)
                {
                    try { mMaintenanceCTS.Cancel(); }
                    catch { }
                }

                if (mMaintenanceTask != null)
                {
                    // wait for the task to exit before disposing it
                    try { mMaintenanceTask.Wait(); }
                    catch { }

                    try { mMaintenanceTask.Dispose(); }
                    catch { }
                }

                if (mMaintenanceCTS != null)
                {
                    try { mMaintenanceCTS.Dispose(); }
                    catch { }
                }

                try
                {
                    var lContext = mRootContext.NewMethod(nameof(cSectionCache), nameof(Dispose));
                    try { ZMaintenance(true, lContext); }
                    catch (Exception e) { lContext.TraceException(e); }
                }
                catch { }
            }

            mDisposed = true;
        }

        internal static bool FileTimesAreTheSame(DateTime pA, DateTime pB)
        {
            // daylight saving time can cause issues
            var lDiff = pA - pB;
            return lDiff == TimeSpan.Zero || lDiff == kPlusOneHour || lDiff == kMinusOneHour;
        }
    }
}