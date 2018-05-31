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

        private readonly ConcurrentDictionary<cSectionCachePersistentKey, cSectionCacheItem> mPersistentKeyItems = new ConcurrentDictionary<cSectionCachePersistentKey, cSectionCacheItem>();
        private readonly ConcurrentDictionary<cSectionCacheNonPersistentKey, cSectionCacheItem> mNonPersistentKeyItems = new ConcurrentDictionary<cSectionCacheNonPersistentKey, cSectionCacheItem>();

        // lock and collections for expiring cache items 
        private readonly object mExpiredLock = new object();
        private HashSet<iMessageHandle> mExpunged = new HashSet<iMessageHandle>();
        private Dictionary<cSectionCacheMailboxId, uint> mUIDValiditiesDiscovered = new Dictionary<cSectionCacheMailboxId, uint>();

        // pending items
        private readonly object mPendingItemsLock = new object();
        private readonly HashSet<cSectionCacheItem> mPendingPersistentKeyItems = new HashSet<cSectionCacheItem>();
        private readonly HashSet<cSectionCacheItem> mPendingNonPersistentKeyItems = new HashSet<cSectionCacheItem>();

        private int mItemSequence = 7;
        private Task mBackgroundTask = null;

        protected cSectionCache(string pInstanceName, int pMaintenanceFrequency)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            if (MaintenanceFrequency < 1000) throw new ArgumentOutOfRangeException(nameof(pMaintenanceFrequency));
            MaintenanceFrequency = pMaintenanceFrequency;
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
        }

        // should be called by the derived class to start the maintenance task
        protected void StartMaintenance()
        {
            if (mBackgroundTask != null) throw new InvalidOperationException();
            mBackgroundTask = ZBackgroundTaskAsync(mBackgroundCancellationTokenSource.Token, mRootContext);
        }

        // asks the cache to create a new item
        //
        protected abstract cSectionCacheItem YGetNewItem(cSectionCacheMailboxId pMailboxId, cTrace.cContext pParentContext);

        // asks the cache if it has an item for the key
        //
        protected virtual bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetExistingItem), pKey);
            rItem = null;
            return false;
        }

        // tells the cache that it might want to copy any cached data to exist under a new UID
        //
        protected internal virtual void Copied(cAccountId pAccountId, cMailboxName pSourceMailboxName, cMailboxName pDestinationMailboxName, cCopyFeedback pCopyFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Copied), pAccountId, pSourceMailboxName, pDestinationMailboxName, pCopyFeedback);
        }

        protected virtual void Maintenance(cSectionCacheMaintenanceInfo pInfo, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Maintenance));
        }

        public bool IsDisposed => mDisposed || mDisposing;

        protected internal int GetItemSequence() => Interlocked.Increment(ref mItemSequence);

        internal void Expunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Expunged), pMessageHandle);
            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            lock (mExpiredLock) { mExpunged.Add(pMessageHandle); }
        }

        internal void Expunged(cMessageHandleList pMessageHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Expunged), pMessageHandles);
            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
            if (pMessageHandles.Count == 0) return;
            lock (mExpiredLock) { mExpunged.UnionWith(pMessageHandles); }
        }

        internal void UIDValidityDiscovered(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(Expunged), pMailboxHandle);
            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMailboxHandle.MailboxStatus == null) throw new ArgumentOutOfRangeException(nameof(pMailboxHandle));
            lock (mExpiredLock) { mUIDValiditiesDiscovered[new cSectionCacheMailboxId(pMailboxHandle)] = pMailboxHandle.MailboxStatus.UIDValidity; }
        }

        internal bool TryGetItemLength(cSectionCachePersistentKey pKey, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemLength), pKey);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext, 1);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));

            if (mPersistentKeyItems.TryGetValue(pKey, out var lPKItem))
            {
                rLength = lPKItem.Length;
                return true;
            }

            if (TryGetExistingItem(pKey, out var lExistingItem, lContext))
            {
                if (lExistingItem == null || !lExistingItem.Cached) throw new cUnexpectedSectionCacheActionException(lContext, 2);

                mPersistentKeyItems.TryAdd(pKey, lExistingItem);
                rLength = lExistingItem.Length;
                return true;
            }

            rLength = -1;
            return false;
        }

        internal bool TryGetItemReader(cSectionCachePersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetItemReader), pKey);

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

                if (lExistingItem.ItemKey == lPKItem.ItemKey)
                {
                    rReader = null;
                    return false;
                }

                if (lPKItem == null) mPersistentKeyItems.TryAdd(pKey, lExistingItem);
                else mPersistentKeyItems.TryUpdate(pKey, lExistingItem, lPKItem);
            }
        }

        internal cSectionCacheItem GetNewItem(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
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

        internal bool TryGetItemLength(cSectionCacheNonPersistentKey pKey, out long rLength, cTrace.cContext pParentContext)
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

        internal bool TryGetItemReader(cSectionCacheNonPersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
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

        internal cSectionCacheItem GetNewItem(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
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
            Dictionary<cSectionCacheMailboxId, uint> lUIDValiditiesDiscovered;

            lock (mExpiredLock)
            {
                lExpungedMessageHandles = mExpunged;
                mExpunged = new HashSet<iMessageHandle>();
                lUIDValiditiesDiscovered = mUIDValiditiesDiscovered;
                mUIDValiditiesDiscovered = new Dictionary<cSectionCacheMailboxId, uint>();
            }

            HashSet<cSectionCacheMessageId> lExpungedMessageIds = new HashSet<cSectionCacheMessageId>();
            foreach (var lMessageHandle in mExpunged) if (lMessageHandle.UID != null) lExpungedMessageIds.Add(new cSectionCacheMessageId(lMessageHandle));

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
                    if (lPKItem.ItemKey == lNPKItem.ItemKey) lNPKItem.SetIndexed(lContext);
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