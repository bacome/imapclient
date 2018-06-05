using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    // currently copy/ rename do not result in cached items being copied to the new UID/ mailbox name(s)
    //  if this were to be done it has to be done at the time of the copy/ rename in case the copied-to mailbox is selected
    //   (if rename is implemented remember the exceptional processing for rename INBOX)

    public abstract class cSectionCache : IDisposable
    {
        private static readonly TimeSpan kPlusOneHour = TimeSpan.FromHours(1);
        private static readonly TimeSpan kMinusOneHour = TimeSpan.FromHours(-1);

        private bool mDisposed = false;
        private bool mDisposing = false;

        public readonly string InstanceName;
        public readonly int MaintenanceFrequency;

        protected readonly cTrace.cContext mRootContext;

        private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource();

        private readonly ConcurrentDictionary<cSectionId, cSectionCacheItem> mSectionIdToItem = new ConcurrentDictionary<cSectionId, cSectionCacheItem>();
        private readonly ConcurrentDictionary<cSectionHandle, cSectionCacheItem> mSectionHandleToItem = new ConcurrentDictionary<cSectionHandle, cSectionCacheItem>();

        // lock and collections for expiring cache items 
        private readonly object mExpiredLock = new object();
        private HashSet<iMessageHandle> mExpungedMessages = new HashSet<iMessageHandle>();
        private Dictionary<cMailboxId, uint> mMailboxToUIDValidity = new Dictionary<cMailboxId, uint>();

        // pending items: new items that haven't been added yet
        private readonly object mPendingItemsLock = new object();
        private readonly HashSet<cSectionCacheItem> mPendingIdItems = new HashSet<cSectionCacheItem>();
        private readonly HashSet<cSectionCacheItem> mPendingHandleItems = new HashSet<cSectionCacheItem>();

        private int mItemSequence = 7;
        private Task mBackgroundTask = null;

        protected cSectionCache(string pInstanceName, int pMaintenanceFrequency)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            if (MaintenanceFrequency < 1000) throw new ArgumentOutOfRangeException(nameof(pMaintenanceFrequency));
            MaintenanceFrequency = pMaintenanceFrequency;
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
        }

        // should be called by the derived class at the end of construction before cache use starts
        //
        protected void StartMaintenance()
        {
            if (mBackgroundTask != null) throw new InvalidOperationException();
            mBackgroundTask = ZBackgroundTaskAsync(mBackgroundCancellationTokenSource.Token, mRootContext);
        }

        // asks the cache to create a new item
        //
        protected abstract cSectionCacheItem YGetNewItem(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);

        public bool IsDisposed => mDisposed || mDisposing;

        // asks the cache if it has an item for the section
        //
        protected virtual bool TryGetExistingItem(cSectionId pSectionId, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetExistingItem), pSectionId);
            rItem = null;
            return false;
        }

        protected virtual void Maintenance(cSectionCacheMaintenanceInfo pInfo, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Maintenance));
        }

        // gives the cache a chance to copy any cached items it has
        //  [NOTE: must be called by an internal that does similar processing on the internal lists]
        //  [uid copy and copy]
        //
        protected virtual void YCopy(cMailboxId pMailboxId, cCopyFeedback pCopyFeedback, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YCopy), pMailboxId, pCopyFeedback, pMailboxName);
        }

        // gives the cache a chance to copy any cached items it has
        //  [NOTE: must be called by an internal that does similar processing on the internal lists]
        //  [NOTE that a delete will be scheduled for the renamed mailbox]
        //  [NOTE that this will never be called for the INBOX]
        //
        protected virtual void YRename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(YRename), pMailboxId, pUIDValidity, pMailboxName);
        }

        // asks the cache to return a list of UIDs that it has for a mailbox
        //
        protected internal virtual List<uint> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetUIDs), pMailboxId, pUIDValidity);
            return null;
        }

        // asks the cache to return a list of child mailboxes that it has for a mailbox
        //
        protected internal virtual List<cMailboxName> GetChildMailboxes(cMailboxId pMailboxId, bool pDescend, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetChildMailboxes), pMailboxId);
            return null;
        }

        protected internal int GetItemSequence() => Interlocked.Increment(ref mItemSequence);

        internal void AddExpungedMessage(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(AddExpungedMessage), pMessageHandle);
            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            lock (mExpiredLock) { mExpungedMessages.Add(pMessageHandle); }
        }

        internal void AddExpungedMessages(cMessageHandleList pMessageHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(AddExpungedMessages), pMessageHandles);
            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
            if (pMessageHandles.Count == 0) return;
            lock (mExpiredLock) { mExpungedMessages.UnionWith(pMessageHandles); }
        }

        internal void AddMailboxUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            // NOTE that to delete all the items in the mailbox the UIDValidity can be set to zero
            //  this happens during child reconcilliation (list), rename and delete
            //   note that rename of inbox is problematic and has to be handled by add expunged
            //   rename of non inbox deletes the renamed AND all children (and children of children ...)
            //   delete just deletes the deleted mailbox
            //   note that discovering that a mailbox is noselect or non-existent implies that the UIDValidity is zero (this is done during list)
            //
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(AddMailboxUIDValidity), pMailboxId, pUIDValidity);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            lock (mExpiredLock) { mMailboxToUIDValidity[pMailboxId] = pUIDValidity; }
        }

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

        private async Task ZBackgroundTaskAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZBackgroundTaskAsync));

            try
            {
                while (true)
                {
                    ZMaintenance(pCancellationToken, lContext);
                    lContext.TraceVerbose("waiting: {0}", MaintenanceFrequency);
                    await Task.Delay(MaintenanceFrequency, pCancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (!pCancellationToken.IsCancellationRequested && lContext.TraceException("the background task is stopping due to an unexpected error", e)) { }
        }

        private void ZMaintenance(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewRootMethod(nameof(cSectionCache), nameof(ZMaintenance));

            // take a copy and replace the sets that tell us about expired items

            HashSet<iMessageHandle> lExpungedMessageHandles;
            Dictionary<cMailboxId, uint> lUIDValiditiesDiscovered;

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

                if (mBackgroundCancellationTokenSource != null && !mBackgroundCancellationTokenSource.IsCancellationRequested)
                {
                    try { mBackgroundCancellationTokenSource.Cancel(); }
                    catch { }
                }

                if (mBackgroundTask != null)
                {
                    // wait for the task to exit before disposing it
                    try { mBackgroundTask.Wait(); }
                    catch { }

                    try { mBackgroundTask.Dispose(); }
                    catch { }
                }

                if (mBackgroundCancellationTokenSource != null)
                {
                    try { mBackgroundCancellationTokenSource.Dispose(); }
                    catch { }
                }

                try
                {
                    var lContext = mRootContext.NewMethod(nameof(cSectionCache), nameof(Dispose));
                    try { ZMaintenance(CancellationToken.None, lContext); }
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