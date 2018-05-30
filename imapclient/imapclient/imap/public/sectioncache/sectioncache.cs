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

        private readonly object mLock = new object();
        private HashSet<iMessageHandle> mExpunged = new HashSet<iMessageHandle>();
        private Dictionary<cSectionCacheMailboxId, uint> mUIDValiditiesDiscovered = new Dictionary<cSectionCacheMailboxId, uint>();

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
        protected abstract cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext);

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





        ;?; // uidvalidity, expunge, copy

        internal void Expunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            ;?; // these should be honoured in maintenance only
            ;?; // and if it can't be the request to delete should be recorded somehow against the item, the cache won't remember
            ;?; // the expunged items should have account/.../UID
            ;?; // the uidvals account/../uidvalid


            ;?;
            // note that this must trydelete(-2) on npk and pk items before passing on the request if it has a UID
        }

        internal void Expunged(cMessageHandleList pMessageHandles, cTrace.cContext pParentContext)
        {
            ;?; // these should be honoured in maintenance only
            ;?;
            // note that this must trydelete(-2) on npk and pk items before passing on the requests that have UIDs
        }

        internal void UIDValidityDiscovered(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
        {
            ;?; // these should be honoured in maintenance only
            ;?;
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

            foreach (var lPair in mNonPersistentKeyItems)
            {
                var lNPKItem = lPair.Value;

                if (lNPKItem.Deleted || lNPKItem.Indexed) continue;

                if (pKey.Equals(lPair.Key))
                {
                    if (mPersistentKeyItems.TryAdd(pKey, lNPKItem)) lNPKItem.SetIndexed(lContext);
                    rLength = lNPKItem.Length;
                    return true;
                }
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

            bool lSomeThingChanged = true;

            while (lSomeThingChanged)
            {
                cSectionCacheItem lPKItem = null;

                if (mPersistentKeyItems.TryGetValue(pKey, out lPKItem)) if (lPKItem.TryGetReader(out rReader, lContext)) return true;

                if (TryGetExistingItem(pKey, out var lExistingItem, lContext))
                {
                    if (lExistingItem == null || !lExistingItem.Cached) throw new cUnexpectedSectionCacheActionException(lContext, 2);

                    if (lExistingItem.ItemKey != lPKItem.ItemKey)
                    {
                        if (lPKItem == null)
                        {
                            if (mPersistentKeyItems.TryAdd(pKey, lExistingItem))
                            {
                                lPKItem = lExistingItem;
                                if (lPKItem.TryGetReader(out rReader, lContext)) return true;
                            }
                            else continue; // something changed
                        }
                        else
                        {
                            if (mPersistentKeyItems.TryUpdate(pKey, lExistingItem, lPKItem))
                            {
                                lPKItem = lExistingItem;
                                if (lPKItem.TryGetReader(out rReader, lContext)) return true;
                            }
                            else continue; // something changed
                        }
                    }
                }

                lSomeThingChanged = false;

                foreach (var lPair in mNonPersistentKeyItems)
                {
                    var lNPKItem = lPair.Value;

                    if (lNPKItem.Deleted || lNPKItem.Indexed) continue;

                    if (pKey.Equals(lPair.Key))
                    {
                        if (lNPKItem.ItemKey == lPKItem.ItemKey) lNPKItem.SetIndexed(lContext);
                        else
                        {
                            if (lPKItem == null)
                            {
                                if (mPersistentKeyItems.TryAdd(pKey, lNPKItem))
                                {
                                    lNPKItem.SetIndexed(lContext);
                                    lPKItem = lNPKItem;
                                    if (lPKItem.TryGetReader(out rReader, lContext)) return true;
                                }
                                else
                                {
                                    lSomeThingChanged = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (mPersistentKeyItems.TryUpdate(pKey, lNPKItem, lPKItem))
                                {
                                    lNPKItem.SetIndexed(lContext);
                                    lPKItem = lNPKItem;
                                    if (lPKItem.TryGetReader(out rReader, lContext)) return true;
                                }
                                else
                                {
                                    lSomeThingChanged = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            rReader = null;
            return false;
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

        internal cSectionCacheItem GetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewItem));
            if (IsDisposed) throw new ObjectDisposedException(nameof(cSectionCache));
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) throw new cSectionCacheException("background task has stopped", mBackgroundTask.Exception, lContext);
            var lItem = YGetNewItem(lContext);
            if (lItem == null || !lItem.CanGetReaderWriter) throw new cUnexpectedSectionCacheActionException(lContext);
            return lItem;
        }

        internal void AddItem(cSectionCachePersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(AddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            if (IsDisposed) return;
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) return;

            if (mPersistentKeyItems.TryGetValue(pKey, out var lPKItem))
            {
                if (lPKItem.TryTouch(lContext)) return;
                if (mPersistentKeyItems.TryUpdate(pKey, pItem, lPKItem)) pItem.SetCached(pKey, lContext);
                return;
            }

            if (mPersistentKeyItems.TryAdd(pKey, pItem)) pItem.SetCached(pKey, lContext);
        }

        internal void AddItem(cSectionCacheNonPersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(AddItem), pKey, pItem);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (pItem.Cache != this || pItem.Cached) throw new ArgumentOutOfRangeException(nameof(pItem));

            if (IsDisposed) return;
            if (mBackgroundTask == null) throw new cUnexpectedSectionCacheActionException(lContext);
            if (mBackgroundTask.IsCompleted) return;

            if (mNonPersistentKeyItems.TryGetValue(pKey, out var lNPKItem))
            {
                if (lNPKItem.TryTouch(lContext)) return;
                if (mNonPersistentKeyItems.TryUpdate(pKey, pItem, lNPKItem)) pItem.SetCached(pKey, lContext);
                return;
            }

            if (mNonPersistentKeyItems.TryAdd(pKey, pItem)) pItem.SetCached(pKey, lContext);
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

            // 

            ;?; // build the maintenance info that we are going to use this time from the synchronised queues

            // delete duplicates and invalids, index items that can be indexed

            foreach (var lPair in mNonPersistentKeyItems)
            {
                var lNPKItem = lPair.Value;

                if (lNPKItem.Deleted || lNPKItem.ToBeDeleted || lNPKItem.Indexed) continue;

                ;?; // check if it is on the deleted list and delete
                ;?; // check if the UIDvalidity is wrong and dekete


                if (lNPKItem.PersistentKey == null)
                {
                    if (mDisposing || !lPair.Key.IsValidToCache)
                    {
                        lNPKItem.TryDelete(-2, lContext);
                        if (pCancellationToken.IsCancellationRequested) return;
                    }

                    continue;
                }

                if (mPersistentKeyItems.TryGetValue(lNPKItem.PersistentKey, out var lPKItem))
                {
                    if (lPKItem.ItemKey == lNPKItem.ItemKey) lNPKItem.SetIndexed(lContext);
                    else
                    {
                        if (lPKItem.TryTouch(lContext)) lNPKItem.TryDelete(-2, lContext);
                        else if (mPersistentKeyItems.TryUpdate(lNPKItem.PersistentKey, lNPKItem, lPKItem)) lNPKItem.SetIndexed(lContext);
                        if (pCancellationToken.IsCancellationRequested) return;
                    }
                }
                else if (mPersistentKeyItems.TryAdd(lNPKItem.PersistentKey, lNPKItem)) lNPKItem.SetIndexed(lContext);
            }

            if (pCancellationToken.IsCancellationRequested) return;

            // assign pks

            foreach (var lPair in mPersistentKeyItems)
            {
                var lPKItem = lPair.Value;

                if (lPKItem.Deleted || lPKItem.ToBeDeleted) continue;

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