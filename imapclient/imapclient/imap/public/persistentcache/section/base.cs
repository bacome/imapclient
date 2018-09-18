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
        private static readonly TimeSpan kPlusOneHour = TimeSpan.FromHours(1);
        private static readonly TimeSpan kMinusOneHour = TimeSpan.FromHours(-1);

        ;?; // null entries => no: remove them


        ;?; // rmeove disposable
        private bool mDisposed = false;
        private bool mDisposing = false;

        public readonly string InstanceName;
        protected readonly cTrace.cContext mRootContext;

        // collections for storing cache items; note that the values here may be null (maintenance will remove entries with null values, values can be set to null by rename)
        private readonly ConcurrentDictionary<cSectionHandle, cSectionCacheItem> mSectionHandleToItem = new ConcurrentDictionary<cSectionHandle, cSectionCacheItem>();
        private readonly ConcurrentDictionary<cSectionId, cSectionCacheItem> mSectionIdToItem = new ConcurrentDictionary<cSectionId, cSectionCacheItem>();

        // pending items: new items that haven't been added to the cache yet (items are in this collection from when getnewitem is called until additem is called)
        private readonly ConcurrentDictionary<cSectionCacheItem, byte> mPendingItems = new ConcurrentDictionary<cSectionCacheItem, byte>();

        // source for numbering cache items
        private int mItemSequence = 7;

        protected cSectionCache(string pInstanceName)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
        }

        protected internal override HashSet<cUID> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetUIDs), pMailboxId, pUIDValidity);

            // note that this may return UIDs that we know are pending delete
            //  I could filter them out, but how would the override in the derived class do it?
            //   so for consistency, I don't do it

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            YObjectStateCheck(lContext);

            var lUIDs = new HashSet<cUID>();

            // the order of these loops is important to ensure that we don't miss a message when it is being moved from pending to added

            foreach (var lPair in mPendingItems)
            {
                var lItem = lPair.Key;
                if (lItem.SectionHandle == null) LAddFromUID(lItem.SectionId.MessageUID, lItem);
                else LAddFromHandle(lItem.SectionHandle.MessageHandle, lItem);
            }

            foreach (var lPair in mSectionHandleToItem) LAddFromHandle(lPair.Key.MessageHandle, lPair.Value);
            foreach (var lPair in mSectionIdToItem) LAddFromUID(lPair.Key.MessageUID, lPair.Value);

            return lUIDs;

            void LAddFromUID(cMessageUID pMessageUID, cSectionCacheItem pItem)
            {
                if (pMessageUID.MailboxId == pMailboxId && pMessageUID.UID.UIDValidity == pUIDValidity && pItem != null && !pItem.Deleted && !pItem.ToBeDeleted) lUIDs.Add(pMessageUID.UID);
            }

            void LAddFromHandle(iMessageHandle pMessageHandle, cSectionCacheItem pItem)
            {
                if (!pMessageHandle.Expunged && pMessageHandle.UID != null && pMessageHandle.MessageCache.MailboxHandle.MailboxId == pMailboxId && pMessageHandle.UID.UIDValidity == pUIDValidity && pItem != null && pItem.IndexedBySectionId == eSectionCacheIndexedBySectionId.notindexed && !pItem.Deleted && !pItem.ToBeDeleted) lUIDs.Add(pMessageHandle.UID);
            }
        }

        public override void MessageExpunged(cMailboxId pMailboxId, cUID pUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(MessageExpunged), pMailboxId, pUID);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            YObjectStateCheck(lContext);
            mExpungedMessages.TryAdd(new cMessageUID(pMailboxId, pUID), cASCII.NUL);
        }

        public override void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(MessagesExpunged), pMailboxId);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            YObjectStateCheck(lContext);
            foreach (var lUID in pUIDs) mExpungedMessages.TryAdd(new cMessageUID(pMailboxId, lUID), cASCII.NUL);
        }

        public override void SetMailboxUIDValidity(cMailboxId pMailboxId, long pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(SetMailboxUIDValidity), pMailboxId, pUIDValidity);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity < -1) throw new ArgumentOutOfRangeException(nameof(pUIDValidity)); // -1 means that the mailbox has been deleted
            YObjectStateCheck(lContext);
            mMailboxIdToUIDValidity[pMailboxId] = pUIDValidity;
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

            // note that this may copy UIDs that we know are pending delete (another client could have sent us the delete notification)
            //  I could filter them out, but how would the override in the derived class do it?
            //   so for consistency, I don't do it
            //    [this means that the item may not be deleted until the next time a reconciliation is done]

            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            YObjectStateCheck(lContext);

            if (!YCanCopy) return;

            var lDestinationMailboxId = new cMailboxId(pSourceMailboxId.AccountId, pDestinationMailboxName);

            var lCopiedSectionIds = new HashSet<cSectionId>();

            foreach (var lPair in mSectionHandleToItem)
            {
                var lMessageHandle = lPair.Key.MessageHandle;
                var lItem = lPair.Value;

                if (lItem != null && lItem.IndexedBySectionId == eSectionCacheIndexedBySectionId.notindexed && !lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && !lMessageHandle.Expunged && lMessageHandle.UID != null && lMessageHandle.MessageCache.MailboxHandle.MailboxId == pSourceMailboxId && pFeedback.TryGetValue(lMessageHandle.UID, out var lCreatedUID))
                {
                    if (lCopiedSectionIds.Contains(lItem.SectionId)) continue; // could happen if there was a duplicate

                    var lNewSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lCreatedUID), lPair.Key.Section, lPair.Key.Decoding);

                    if (lItem.TryCopy(lNewSectionId, out var lNewItem, lContext))
                    {
                        lCopiedSectionIds.Add(lItem.SectionId);
                        if (!mSectionIdToItem.TryAdd(lNewSectionId, lNewItem)) lNewItem.Delete(lContext);
                    }
                }
            }

            foreach (var lPair in mSectionIdToItem)
            {
                var lMessageUID = lPair.Key.MessageUID;
                var lItem = lPair.Value;

                if (lItem != null && !lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && lMessageUID.MailboxId == pSourceMailboxId && pFeedback.TryGetValue(lMessageUID.UID, out var lCreatedUID))
                {
                    if (lCopiedSectionIds.Contains(lPair.Key)) continue; // could happen if we found one in the previous loop

                    var lNewSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lCreatedUID), lPair.Key.Section, lPair.Key.Decoding);

                    if (lItem.TryCopy(lNewSectionId, out var lNewItem, lContext))
                    {
                        if (!mSectionIdToItem.TryAdd(lNewSectionId, lNewItem)) lNewItem.Delete(lContext);
                    }
                }
            }
        }

        protected override HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YGetMailboxNames), pAccountId);

            // note that this may return mailbox names that we know are pending delete
            //  I could filter them out, but how would the override in the derived class do it?
            //   so for consistency, I don't do it

            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));

            YObjectStateCheck(lContext);

            var lMailboxNames = new HashSet<cMailboxName>();

            // the order of these loops is important to ensure that we don't miss a message when it is being moved from pending to the dictionaries

            foreach (var lPair in mPendingItems)
            {
                var lItem = lPair.Key;
                if (lItem.SectionHandle == null) LAddFromUID(lItem.SectionId.MessageUID.MailboxId, lItem);
                else LAddFromHandle(lItem.SectionHandle.MessageHandle, lItem);
            }

            foreach (var lPair in mSectionHandleToItem) LAddFromHandle(lPair.Key.MessageHandle, lPair.Value);
            foreach (var lPair in mSectionIdToItem) LAddFromUID(lPair.Key.MessageUID.MailboxId, lPair.Value);

            return lMailboxNames;

            void LAddFromUID(cMailboxId pMailboxId, cSectionCacheItem pItem)
            {
                if (pMailboxId.AccountId == pAccountId && pItem != null && !pItem.Deleted && !pItem.ToBeDeleted) lMailboxNames.Add(pMailboxId.MailboxName);
            }

            void LAddFromHandle(iMessageHandle pMessageHandle, cSectionCacheItem pItem)
            {
                if (pMessageHandle.Expunged) return;
                var lMailboxId = pMessageHandle.MessageCache.MailboxHandle.MailboxId;
                if (lMailboxId.AccountId == pAccountId && pItem != null && pItem.IndexedBySectionId == eSectionCacheIndexedBySectionId.notindexed && !pItem.Deleted && !pItem.ToBeDeleted) lMailboxNames.Add(lMailboxId.MailboxName);
            }
        }

        protected override void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            // the sub-class should call this before doing its rename (in case maintenance is assigning keys while this is running)
            // the sub-class must defend against duplicates being created by any renames it does (due to another client querying the item or due to a duplicate entry (one persisted and one in handles: a rename of the handle one and a maintenance run persisting it)

            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YRename), pMailboxId, pMailboxName);

            // note that this may rename UIDs that we know are pending delete (another client could have sent us the delete)
            //  I could filter them out, but how would the override in the derived class do it?
            //   so for consistency, I don't do it
            //    [this means that the item may not be deleted until the next time a reconciliation is done]

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            YObjectStateCheck(lContext);

            var lDestinationMailboxId = new cMailboxId(pMailboxId.AccountId, pMailboxName);

            var lRenamedSectionIds = new HashSet<cSectionId>();

            foreach (var lPair in mSectionHandleToItem)
            {
                var lMessageHandle = lPair.Key.MessageHandle;
                var lItem = lPair.Value;

                if (lItem != null && lItem.IndexedBySectionId == eSectionCacheIndexedBySectionId.notindexed && !lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && !lMessageHandle.Expunged && lMessageHandle.UID != null && lMessageHandle.MessageCache.MailboxHandle.MailboxId == pMailboxId)
                {
                    if (!mSectionHandleToItem.TryUpdate(lPair.Key, null, lItem)) continue; // the value has been changed, probably by a messagedatastream completing and the item having been deleted in the mean time (i.e. next to impossible)
                    if (lRenamedSectionIds.Contains(lItem.SectionId)) continue; // could happen if there was a duplicate

                    var lOldSectionId = lItem.SectionId;
                    var lNewSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lMessageHandle.UID), lPair.Key.Section, lPair.Key.Decoding);

                    if (lItem.TryRename(lNewSectionId, lContext))
                    {
                        lRenamedSectionIds.Add(lOldSectionId);
                        if (!mSectionIdToItem.TryAdd(lNewSectionId, lItem)) lItem.Delete(lContext);
                    }
                }
            }

            foreach (var lPair in mSectionIdToItem)
            {
                var lMessageUID = lPair.Key.MessageUID;
                var lItem = lPair.Value;

                if (lItem != null && !lItem.Deleted && !lItem.ToBeDeleted && lItem.PersistState != eSectionCachePersistState.persisted && lMessageUID.MailboxId == pMailboxId)
                {
                    if (!mSectionIdToItem.TryUpdate(lPair.Key, null, lItem)) continue;
                    if (lRenamedSectionIds.Contains(lPair.Key)) continue; // could happen if we found one in the previous loop
                    if (lItem.SectionId != lPair.Key) continue; // already renamed - could happen if maintenance was indexing while this was running

                    var lNewSectionId = new cSectionId(new cMessageUID(lDestinationMailboxId, lMessageUID.UID), lPair.Key.Section, lPair.Key.Decoding);

                    if (lItem.TryRename(lNewSectionId, lContext))
                    {
                        if (!mSectionIdToItem.TryAdd(lNewSectionId, lItem)) lItem.Delete(lContext);
                    }
                }
            }
        }

        public bool TryGetSectionLength(cSectionId pSectionId, out long rLength) => TryGetSectionLength(pSectionId, out rLength, mRootContext);

        public bool TryGetSectionReader(cSectionId pSectionId, out Stream rStream)
        {
            if (TryGetSectionReader(pSectionId, out var lReader, mRootContext))
            {
                rStream = lReader;
                return true;
            }

            rStream = null;
            return false;
        }

        public bool IsDisposed => mDisposed || mDisposing;

        // asks the cache to create a new item
        //
        protected abstract cSectionCacheItem YGetNewItem(cMailboxId pMailboxId, uint pUIDValidity, bool pUIDNotSticky, cTrace.cContext pParentContext);

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

        internal bool TryGetSectionLength(cSectionId pSectionId, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemLength), pSectionId);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            YObjectStateCheck(lContext);

            if (ZMessageIsToBeDeleted(pSectionId.MessageUID))
            {
                rLength = -1;
                return false;
            }

            var lFoundAValue = mSectionIdToItem.TryGetValue(pSectionId, out var lDictionaryItem);

            if (lFoundAValue && lDictionaryItem != null && !lDictionaryItem.Deleted && !lDictionaryItem.ToBeDeleted)
            {
                rLength = lDictionaryItem.Length;
                return true;
            }

            cSectionCacheItem lExistingItem;

            try
            {
                if (!YTryGetExistingItem(pSectionId, out lExistingItem, lContext))
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

            ZExistingItemCheck(lExistingItem, lContext);

            if (lDictionaryItem != null && lExistingItem.ItemId.Equals(lDictionaryItem.ItemId))
            {
                rLength = -1;
                return false;
            }

            if (lFoundAValue) mSectionIdToItem.TryUpdate(pSectionId, lExistingItem, lDictionaryItem);
            else mSectionIdToItem.TryAdd(pSectionId, lExistingItem);

            rLength = lExistingItem.Length;
            return true;
        }

        internal bool TryGetSectionReader(cSectionId pSectionId, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pSectionId);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            YObjectStateCheck(lContext);

            if (ZMessageIsToBeDeleted(pSectionId.MessageUID))
            {
                rReader = null;
                return false;
            }

            while (true)
            {
                var lFoundAValue = mSectionIdToItem.TryGetValue(pSectionId, out var lDictionaryItem);

                if (lFoundAValue && lDictionaryItem != null && lDictionaryItem.TryGetReader(out rReader, lContext)) return true;

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

                ZExistingItemCheck(lExistingItem, lContext);

                if (lDictionaryItem != null && lExistingItem.ItemId.Equals(lDictionaryItem.ItemId))
                {
                    rReader = null;
                    return false;
                }

                if (lFoundAValue) mSectionIdToItem.TryUpdate(pSectionId, lExistingItem, lDictionaryItem);
                else mSectionIdToItem.TryAdd(pSectionId, lExistingItem);
            }
        }

        internal cSectionCacheItem GetNewItem(cSectionId pSectionId, bool pUIDNotSticky, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewItem), pSectionId, pUIDNotSticky);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            YObjectStateCheck(lContext);

            var lNewItem = YGetNewItem(pSectionId.MessageUID.MailboxId, pSectionId.MessageUID.UID.UIDValidity, pUIDNotSticky, lContext);
            ZNewItemCheck(lNewItem, lContext);
            lNewItem.SetSectionId(pSectionId, lContext);

            if (!mPendingItems.TryAdd(lNewItem, cASCII.NUL)) throw new cUnexpectedSectionCacheActionException(lContext);

            return lNewItem;
        }

        internal bool TryGetSectionLength(cSectionHandle pSectionHandle, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemLength), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            YObjectStateCheck(lContext);

            if (mSectionHandleToItem.TryGetValue(pSectionHandle, out var lItem) && lItem != null && !lItem.Deleted && !lItem.ToBeDeleted)
            {
                rLength = lItem.Length;
                return true;
            }

            rLength = -1;
            return false;
        }

        internal bool TryGetSectionReader(cSectionHandle pSectionHandle, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            YObjectStateCheck(lContext);

            if (mSectionHandleToItem.TryGetValue(pSectionHandle, out var lItem) && lItem != null) return lItem.TryGetReader(out rReader, lContext);

            rReader = null;
            return false;
        }

        internal cSectionCacheItem GetNewItem(cSectionHandle pSectionHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewItem), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            YObjectStateCheck(lContext);

            var lMailboxHandle = pSectionHandle.MessageHandle.MessageCache.MailboxHandle;

            var lNewItem = YGetNewItem(lMailboxHandle.MailboxId, lMailboxHandle.MailboxStatus?.UIDValidity ?? 0, lMailboxHandle.SelectedProperties.UIDNotSticky ?? true, lContext);
            ZNewItemCheck(lNewItem, lContext);
            lNewItem.SetSectionHandle(pSectionHandle, lContext);

            if (!mPendingItems.TryAdd(lNewItem, cASCII.NUL)) throw new cUnexpectedSectionCacheActionException(lContext);

            return lNewItem;
        }

        internal void TryAddItem(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryAddItem), pItem);

            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (!mPendingItems.ContainsKey(pItem)) throw new ArgumentOutOfRangeException(nameof(pItem));

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
                    pItem.SetNotIndexedBySectionId(lContext);

                    if (mSectionHandleToItem.TryGetValue(pItem.SectionHandle, out var lItem))
                    {
                        if (lItem == null || !lItem.TryTouch(lContext))
                            if (mSectionHandleToItem.TryUpdate(pItem.SectionHandle, pItem, lItem)) pItem.SetNoLongerPending(lContext);
                    }
                    else if (mSectionHandleToItem.TryAdd(pItem.SectionHandle, pItem)) pItem.SetNoLongerPending(lContext);
                }
                else
                {
                    ;?;

                    if (mSectionIdToItem.TryGetValue(lSectionId, out var lItem))
                    {
                        if (lItem == null || !lItem.TryTouch(lContext))
                            if (mSectionIdToItem.TryUpdate(lSectionId, pItem, lItem)) pItem.SetNoLongerPending(lContext);
                    }
                    else if (mSectionIdToItem.TryAdd(lSectionId, pItem)) pItem.SetNoLongerPending(lContext);
                }
            }

            if (!mPendingItems.TryRemove(pItem, out _)) throw new cInternalErrorException(lContext);
        }

        private void ZExistingItemCheck(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            if (pItem == null || pItem.Cache != this || pItem.Pending || pItem.PersistState != eSectionCachePersistState.persisted || pItem.Deleted || pItem.ToBeDeleted) throw new cUnexpectedSectionCacheActionException(pParentContext);
        }

        private void ZNewItemCheck(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            if (pItem == null || pItem.Cache != this || !pItem.CanGetReaderWriter) throw new cUnexpectedSectionCacheActionException(pParentContext);
        }

        private async Task ZMaintenanceAsync(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZMaintenanceAsync));

            try
            {
                while (true)
                {
                    ZMaintenance(lContext);
                    lContext.TraceVerbose("waiting: {0}", MaintenanceFrequency);
                    await Task.Delay(MaintenanceFrequency, mMaintenanceCTS.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (!mMaintenanceCTS.IsCancellationRequested && lContext.TraceException("the background task is stopping due to an unexpected error", e)) { }
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