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

        // lock and collections for recording data used in maintenance to delete items
        private readonly object mExpiredLock = new object();
        private HashSet<cMessageUID> mExpungedMessages = new HashSet<cMessageUID>();
        private Dictionary<cMailboxId, long> mMailboxToUIDValidity = new Dictionary<cMailboxId, long>();

        // collections for storing cache items
        private readonly ConcurrentDictionary<cSectionHandle, cSectionCacheItem> mSectionHandleToItem = new ConcurrentDictionary<cSectionHandle, cSectionCacheItem>();
        private readonly ConcurrentDictionary<cSectionId, cSectionCacheItem> mSectionIdToItem = new ConcurrentDictionary<cSectionId, cSectionCacheItem>();

        // pending items: new items that haven't been added to the cache yet (items are in this collection from when getnewitem is called until additem is called)
        //  [they are here so we can mark them for delete if there is an expunge or uidvalidity change (etc)]
        private readonly object mPendingItemsLock = new object();
        private readonly HashSet<cSectionCacheItem> mPendingItems = new HashSet<cSectionCacheItem>();

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

        public override HashSet<cUID> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetUIDs), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            YObjectStateCheck(lContext);

            var lUIDs = new HashSet<cUID>();

            // when moving from pending it is important to add to the dictionary before removing from pending

            lock (mPendingItemsLock)
            {
                foreach (var lItem in mPendingItems)
                {
                    if (lItem.SectionHandle == null) LAddFromUID(lItem.SectionId.MessageUID, lItem);
                    else LAddFromHandle(lItem.SectionHandle.MessageHandle, lItem);
                }
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

            YObjectStateCheck(lContext);

            var lMessageUID = new cMessageUID(pMailboxId, pUID);

            lock (mExpiredLock) { mExpungedMessages.Add(lMessageUID); }
        }

        public override void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(MessagesExpunged), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            YObjectStateCheck(lContext);

            var lMessageUIDs = new List<cMessageUID>(from lUID in pUIDs select new cMessageUID(pMailboxId, lUID));

            lock (mExpiredLock) { mExpungedMessages.UnionWith(lMessageUIDs); }
        }

        public override void SetMailboxUIDValidity(cMailboxId pMailboxId, long pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(SetMailboxUIDValidity), pMailboxId, pUIDValidity);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            YObjectStateCheck(lContext);
            lock (mExpiredLock) { mMailboxToUIDValidity[pMailboxId] = pUIDValidity; }
        }

        public override void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            // the sub-class should call this before doing its copy (in case maintenance is assigning keys while this is running)
            // the sub-class must defend against duplicates being created by any copies it does (due to assigning keys being done and due to another client querying the item)
            //
            //  this routine has to defend against indexing running at the same time as it (an item may be seen in the pass through mSectionHandleToItem AND the pass through mSectionIdToItem)
            //   AND
            //  this routine has to defend against the copied item already being in cache (due to another client querying it) [in this case delete the item]

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);

            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            YObjectStateCheck(lContext);

            if (!YCanCopy) return;

            var lDestinationMailboxId = new cMailboxId(pSourceMailboxId.AccountId, pDestinationMailboxName);

            var lCopiedSectionIds = new HashSet<cSectionId>();
            var lNewItems = new List<cSectionCacheItem>();

            foreach (var lPair in mSectionHandleToItem)
            {
                var lMessageHandle = lPair.Key.MessageHandle;
                var lItem = lPair.Value;

                if (!lItem.Indexed && !lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && !lMessageHandle.Expunged && lMessageHandle.UID != null && lMessageHandle.MessageCache.MailboxHandle.MailboxId == pSourceMailboxId && pFeedback.TryGetValue(lMessageHandle.UID, out var lCreatedUID))
                {
                    var lSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lCreatedUID), lPair.Key.Section, lPair.Key.Decoding);

                    if (lItem.TryCopy(lSectionId, out var lNewItem, lContext))
                    {
                        lCopiedSectionIds.Add(lItem.SectionId);
                        lNewItems.Add(lNewItem);
                    }
                }
            }

            foreach (var lPair in mSectionIdToItem)
            {
                if (lCopiedSectionIds.Contains(lPair.Key)) continue;

                var lMessageUID = lPair.Key.MessageUID;
                var lItem = lPair.Value;

                if (!lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && lMessageUID.MailboxId == pSourceMailboxId && pFeedback.TryGetValue(lMessageUID.UID, out var lCreatedUID))
                {
                    var lSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lCreatedUID), lPair.Key.Section, lPair.Key.Decoding);
                    if (lItem.TryCopy(lSectionId, out var lNewItem, lContext)) lNewItems.Add(lNewItem);
                }
            }

            foreach (var lItem in lNewItems) if (!mSectionIdToItem.TryAdd(lItem.SectionId, lItem)) lItem.TryDelete(-2, lContext);
        }

        protected override HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YGetMailboxNames), pAccountId);

            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));

            YObjectStateCheck(lContext);

            var lMailboxNames = new HashSet<cMailboxName>();

            // when moving from pending it is important to add to the dictionary before removing from pending

            lock (mPendingItemsLock)
            {
                foreach (var lItem in mPendingItems)
                {
                    if (lItem.SectionHandle == null) LAddFromUID(lItem.SectionId.MessageUID.MailboxId, lItem);
                    else LAddFromHandle(lItem.SectionHandle.MessageHandle, lItem);
                }
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

        protected override void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            // the sub-class should call this before doing its rename (in case maintenance is assigning keys while this is running)
            // the sub-class must defend against duplicates being created by any renames it does (due to another client querying the item or due to a duplicate entry (one persisted and one in handles: a rename of the handle one and a maintenance run persisting it)

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YRename), pMailboxId, pMailboxName);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            YObjectStateCheck(lContext);

            var lDestinationMailboxId = new cMailboxId(pMailboxId.AccountId, pMailboxName);

            var lRenamedItems = new List<cSectionCacheItem>();

            foreach (var lPair in mSectionHandleToItem)
            {
                var lMessageHandle = lPair.Key.MessageHandle;
                var lItem = lPair.Value;

                if (!lItem.Indexed && !lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && !lMessageHandle.Expunged && lMessageHandle.UID != null && lMessageHandle.MessageCache.MailboxHandle.MailboxId == pMailboxId)
                {
                    var lSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lMessageHandle.UID), lPair.Key.Section, lPair.Key.Decoding);
                    if (lItem.TryRename(lSectionId, lContext)) lRenamedItems.Add(lItem);
                }
            }

            foreach (var lPair in mSectionIdToItem)
            {
                var lMessageUID = lPair.Key.MessageUID;
                var lItem = lPair.Value;

                if (!lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && lMessageUID.MailboxId == pMailboxId)
                {
                    var lSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lMessageUID.UID), lPair.Key.Section, lPair.Key.Decoding);
                    if (lItem.TryRename(lSectionId, lContext)) lRenamedItems.Add(lItem);
                }
            }

            foreach (var lItem in lRenamedItems) if (!mSectionIdToItem.TryAdd(lItem.SectionId, lItem)) lItem.TryDelete(-2, lContext);
        }

        // asks the cache to create a new item
        //
        protected abstract cSectionCacheItem YGetNewItem(cMailboxId pMailboxId, uint pUIDValidity, bool pUIDNotSticky, cTrace.cContext pParentContext);

        public bool IsDisposed => mDisposed || mDisposing;

        protected virtual bool YCanCopy => false;
        protected virtual bool YCanPersist => false;

        // asks the cache to return an item for the section if it has one, this default implementation never returns an item 
        //
        protected virtual bool YTryGetExistingItem(cSectionId pSectionId, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YTryGetExistingItem), pSectionId);
            rItem = null;
            return false;
        }

        // gives the cache a chance to do time consuming maintenance
        //
        protected virtual void YMaintenance(bool pFinal, cSectionCacheMaintenanceData pData, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YMaintenance), pFinal, pData);
        }

        protected void YObjectStateCheck(cTrace.cContext pParentContext)
        {
            if (mDisposed || mDisposing) throw new ObjectDisposedException(nameof(cSectionCache));

            if (mMaintenanceTask != null)
            {
                if (mMaintenanceTask.IsCompleted) throw new cSectionCacheException("the maintenance task has stopped", mMaintenanceTask.Exception, pParentContext);
                return;
            }

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

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            YObjectStateCheck(lContext);

            cSectionCacheItem lItem;

            if (mSectionIdToItem.TryGetValue(pSectionId, out lItem))
            {
                rLength = lItem.Length;
                return true;
            }

            try
            {
                if (!YTryGetExistingItem(pSectionId, out lItem, lContext))
                {
                    rLength = -1;
                    return false;
                }
            }
            catch (Exception e)
            {
                lContext.TraceException(nameof(YTryGetExistingItem), e);
                rLength = -1;
                return false;
            }

            if (lItem == null || lItem.Cache != this || !lItem.Cached || lItem.PersistState != eSectionCachePersistState.persisted) throw new cUnexpectedSectionCacheActionException(lContext);

            mSectionIdToItem.TryAdd(pSectionId, lItem);

            rLength = lItem.Length;
            return true;
        }

        internal bool TryGetItemReader(cSectionId pSectionId, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pSectionId);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            YObjectStateCheck(lContext);

            while (true)
            {
                if (mSectionIdToItem.TryGetValue(pSectionId, out var lMyItem)) if (lMyItem.TryGetReader(out rReader, lContext)) return true;

                cSectionCacheItem lExistingItem;

                try
                {
                    if (!YTryGetExistingItem(pSectionId, out lExistingItem, lContext))
                    {
                        rReader = null;
                        return false;
                    }
                }
                catch (Exception e)
                {
                    lContext.TraceException(nameof(YTryGetExistingItem), e);
                    rReader = null;
                    return false;
                }

                if (lExistingItem == null || lExistingItem.Cache != this || !lExistingItem.Cached || lExistingItem.PersistState != eSectionCachePersistState.persisted) throw new cUnexpectedSectionCacheActionException(lContext);

                if (lExistingItem.ItemId.Equals(lMyItem.ItemId))
                {
                    rReader = null;
                    return false;
                }

                if (lMyItem == null) mSectionIdToItem.TryAdd(pSectionId, lExistingItem);
                else mSectionIdToItem.TryUpdate(pSectionId, lExistingItem, lMyItem);
            }
        }

        internal bool TryGetNewItem(cSectionId pSectionId, bool pUIDNotSticky, out cSectionCacheItem rNewItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetNewItem), pSectionId, pUIDNotSticky);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            YObjectStateCheck(lContext);

            try { rNewItem = YGetNewItem(pSectionId.MessageUID.MailboxId, pSectionId.MessageUID.UID.UIDValidity, pUIDNotSticky, lContext); }
            catch (Exception e)
            {
                lContext.TraceException(e);
                rNewItem = null;
                return false;
            }

            if (rNewItem == null || rNewItem.Cache != this || !rNewItem.CanGetReaderWriter) throw new cUnexpectedSectionCacheActionException(lContext);

            rNewItem.SetSectionId(pSectionId, lContext);

            lock (mPendingItemsLock) { mPendingItems.Add(rNewItem); }

            return true;
        }

        internal bool TryGetItemLength(cSectionHandle pSectionHandle, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemLength), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            YObjectStateCheck(lContext);

            if (mSectionHandleToItem.TryGetValue(pSectionHandle, out var lItem))
            {
                rLength = lItem.Length;
                return true;
            }

            rLength = -1;
            return false;
        }

        internal bool TryGetItemReader(cSectionHandle pSectionHandle, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            YObjectStateCheck(lContext);

            if (mSectionHandleToItem.TryGetValue(pSectionHandle, out var lItem)) return lItem.TryGetReader(out rReader, lContext);

            rReader = null;
            return false;
        }

        internal bool TryGetNewItem(cSectionHandle pSectionHandle, out cSectionCacheItem rNewItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetNewItem), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            YObjectStateCheck(lContext);

            var lMailboxHandle = pSectionHandle.MessageHandle.MessageCache.MailboxHandle;

            try { rNewItem = YGetNewItem(lMailboxHandle.MailboxId, lMailboxHandle.MailboxStatus?.UIDValidity ?? 0, lMailboxHandle.SelectedProperties.UIDNotSticky ?? true, lContext); }
            catch (Exception e)
            {
                lContext.TraceException(e);
                rNewItem = null;
                return false;
            }

            if (rNewItem == null || rNewItem.Cache != this || !rNewItem.CanGetReaderWriter) throw new cUnexpectedSectionCacheActionException(lContext);

            rNewItem.SetSectionHandle(pSectionHandle, lContext);

            lock (mPendingItemsLock) { mPendingItems.Add(rNewItem); }

            return true;
        }

        internal void TryAddItem(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryAddItem), pItem);

            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (!mPendingItems.Contains(pItem)) throw new ArgumentOutOfRangeException(nameof(pItem));

            YObjectStateCheck(lContext);

            bool lTryAddItem = !pItem.Deleted && !pItem.ToBeDeleted;

            if (lTryAddItem && pItem.SectionHandle != null)
            {
                var lMessageHandle = pItem.SectionHandle.MessageHandle;

                if (lMessageHandle.Expunged) lTryAddItem = false;
                else if (lMessageHandle.UID == null && !ReferenceEquals(pItem.SectionHandle.Client.SelectedMailboxDetails?.MessageCache, lMessageHandle.MessageCache)) lTryAddItem = false;
            }

            if (lTryAddItem)
            { 
                var lSectionId = pItem.SectionId;

                if (lSectionId == null)
                {
                    if (mSectionHandleToItem.TryGetValue(pItem.SectionHandle, out var lItem))
                    {
                        if (!lItem.TryTouch(lContext) && mSectionHandleToItem.TryUpdate(pItem.SectionHandle, pItem, lItem)) pItem.SetCached(lContext);
                    }
                    else if (mSectionHandleToItem.TryAdd(pItem.SectionHandle, pItem)) pItem.SetCached(lContext);
                }
                else
                {
                    if (mSectionIdToItem.TryGetValue(lSectionId, out var lItem))
                    {
                        if (!lItem.TryTouch(lContext) && mSectionIdToItem.TryUpdate(lSectionId, pItem, lItem)) pItem.SetCached(lContext);
                    }
                    else if (mSectionIdToItem.TryAdd(lSectionId, pItem)) pItem.SetCached(lContext);
                }
            }

            mPendingItems.Remove(pItem);
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

            var lItemsToDelete = new HashSet<cSectionCacheItem>();

            HashSet<cMessageUID> lExpungedMessages;
            Dictionary<cMailboxId, long> lMailboxToUIDValidity;

            lock (mExpiredLock)
            {
                lExpungedMessages = mExpungedMessages;
                mExpungedMessages = new HashSet<cMessageUID>();
                lMailboxToUIDValidity = mMailboxToUIDValidity;
                mMailboxToUIDValidity = new Dictionary<cMailboxId, long>();
            }

            lock (mPendingItemsLock)
            {
                foreach (var lItem in mPendingItems)
                {
                    if (lItem.Deleted || lItem.ToBeDeleted) continue;

                    if (lItem.SectionId != null)
                    {
                        var lMessageUID = lItem.SectionId.MessageUID;
                        if (lExpungedMessages.Contains(lMessageUID)) lItemsToDelete.Add(lItem);
                        else if (lMailboxToUIDValidity.TryGetValue(lMessageUID.MailboxId, out var lUIDValidity) && lMessageUID.UID.UIDValidity != lUIDValidity) lItemsToDelete.Add(lItem);
                    }
                }
            }

            foreach (var lPair in mSectionHandleToItem)
            {
                var lItem = lPair.Value;

                if (lItem.Deleted || lItem.ToBeDeleted) continue;

                var lMessageHandle = lPair.Key.MessageHandle;

                if (lMessageHandle.Expunged)
                {
                    lItemsToDelete.Add(lItem);
                    continue;
                }

                if (lItem.Indexed) continue;

                if (lMessageHandle.UID == null) 
                {
                    if (!ReferenceEquals(lItem.SectionHandle.Client.SelectedMailboxDetails?.MessageCache, lMessageHandle.MessageCache))
                    {
                        lItemsToDelete.Add(lItem);
                        continue;
                    }

                    ;?;
                }
                else
                {
                    // check for uidvalidity and uid

                    // if it is a duplicate, delete it

                    // index it
                }

                ;?;
            }

            // collect duplicate items for deletion

            ;?;

            // delete

            ;?;

            // index items

            ;?;

            // trypersist

            ;?;















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