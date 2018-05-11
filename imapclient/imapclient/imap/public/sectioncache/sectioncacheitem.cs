using System;
using System.IO;
using System.Threading;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cSectionCacheItem
    {
        private static int mItemSequenceSource = 7;

        private readonly object mLock = new object();

        public readonly cSectionCache Cache;
        public readonly string ItemKey;
        public readonly int ItemSequence;
        private readonly Stream mReadWriteStream;
        private bool mCached;
        private bool mPersistentKeyAssigned;

        // incremented when something significant changes about the cache item that should stop it from being deleted if the change wasn't taken into account by the decision to delete
        private int mChangeSequence = 0;

        private int mOpenStreamCount = 0;
        private bool mDeleted = false;
        private bool mIndexed = false;
        private cSectionCachePersistentKey mPersistentKey = null;
        private cSectionCacheNonPersistentKey mNonPersistentKey = null;

        public cSectionCacheItem(cSectionCache pCache, string pItemKey)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            ItemKey = pItemKey ?? throw new ArgumentNullException(nameof(pItemKey));
            ItemSequence = Interlocked.Increment(ref mItemSequenceSource);
            mReadWriteStream = null;
            mCached = true;
            mPersistentKeyAssigned = true;
        }

        public cSectionCacheItem(cSectionCache pCache, string pItemKey, Stream pReadWriteStream)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            ItemKey = pItemKey ?? throw new ArgumentNullException(nameof(pItemKey));
            ItemSequence = Interlocked.Increment(ref mItemSequenceSource);
            mReadWriteStream = pReadWriteStream ?? throw new ArgumentNullException(nameof(pReadWriteStream));
            if (!pReadWriteStream.CanRead || !pReadWriteStream.CanSeek || !pReadWriteStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pReadWriteStream));
            mCached = false;
            mPersistentKeyAssigned = false;
        }

        protected abstract Stream YGetReadStream(cTrace.cContext pParentContext);
        protected abstract void YDelete(cTrace.cContext pParentContext);

        // the persistent key should be set here if it can be set while the item is open
        protected virtual void ItemEncached(long pLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ItemEncached), pLength);
        }

        ;?; // might not be required
        protected virtual void ItemDecached(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ItemDecached));
        }

        // the persistent key should be set here if it can only be set while the item is closed
        protected virtual void Touch(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(Touch));
        }

        // called periodically and in cache closedown if the persistent key is not assigned but one is available
        protected virtual void AssignPersistentKey(bool pItemClosed, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(AssignPersistentKey), pItemClosed);
        }

        public bool Deleted => mDeleted;

        // for use by the derived classes if they delete (probably by renaming) or notice that this item has been deleted
        public void SetDeleted(int pChangeSequence, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetDeleted));

            lock (mLock)
            {
                if (mDeleted)
                {
                    lContext.TraceVerbose("already deleted");
                    return;
                }

                if (pChangeSequence != -1 && pChangeSequence != mChangeSequence)
                {
                    lContext.TraceVerbose("modified");
                    return;
                }

                mDeleted = true;

                if (mCached)
                {
                    try { ItemDecached(lContext); }
                    catch (Exception e) { lContext.TraceException("itemdecached event failure", e); }
                }
            }
        }

        public bool PersistentKeyAssigned => mPersistentKeyAssigned;

        // for use by the derived class when it sets the persistent key
        protected void SetPersistentKeyAssigned(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetDeleted));
            if (!mCached) throw new InvalidOperationException();
            if (mPersistentKeyAssigned) return;
            mPersistentKeyAssigned = true;
        }

        protected internal cSectionCachePersistentKey PersistentKey
        {
            get
            {
                if (mPersistentKey != null) return mPersistentKey;
                if (mNonPersistentKey == null || mNonPersistentKey.UID == null) return null;
                mPersistentKey = new cSectionCachePersistentKey(mNonPersistentKey);
                return mPersistentKey;
            }
        }

        internal cSectionCacheItemReaderWriter GetReaderWriter(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(GetReaderWriter));

            cSectionCacheItemReaderWriter lReaderWriter;

            lock (mLock)
            {
                lReaderWriter = new cSectionCacheItemReaderWriter(mReadWriteStream, ZDecrementOpenStreamCount, lContext);
                mOpenStreamCount++;
            }

            return lReaderWriter;
        }

        internal bool Cached => mCached;

        internal void SetCached(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetCached), pKey);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));

            lock (mLock)
            {
                if (mCached) throw new InvalidOperationException();

                mCached = true;
                mPersistentKey = pKey;

                if (!mDeleted)
                {
                    try { ItemEncached(mReadWriteStream.Length, lContext); }
                    catch (Exception e) { lContext.TraceException("itemcached event failure", e); }
                }
            }
        }

        internal void SetCached(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetCached), pKey);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));

            lock (mLock)
            {
                if (mCached) throw new InvalidOperationException();

                mCached = true;
                mNonPersistentKey = pKey;

                if (!mDeleted)
                {
                    try { ItemEncached(mReadWriteStream.Length, lContext); }
                    catch (Exception e) { lContext.TraceException("itemcached event failure", e); }
                }
            }
        }

        internal bool Indexed => mIndexed;

        internal void SetIndexed(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetIndexed));
            mIndexed = true;
        }

        internal bool TryGetReader(out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryGetReader));

            lock (mLock)
            {
                if (!mCached) throw new InvalidOperationException();

                if (mDeleted)
                {
                    lContext.TraceVerbose("deleted");
                    rReader = null;
                    return false;
                }

                Stream lStream = null;

                try
                {
                    lStream = YGetReadStream(lContext);
                    ;?; // handle null specially
                    if (lStream == null || !lStream.CanRead || !lStream.CanSeek) throw new cUnexpectedSectionCacheActionException(lContext);
                }
                catch (Exception e)
                {
                    lContext.TraceException("failed to get reader", e);

                    if (lStream != null)
                    {
                        try { lStream.Dispose(); }
                        catch (Exception f) { lContext.TraceException("stream dispose failure", f); }
                    }

                    mDeleted = true;

                    try { ItemDecached(lContext); }
                    catch (Exception e2) { lContext.TraceException("itemdecached event failure", e2); }

                    rReader = null;
                    return false;
                }

                rReader = new cSectionCacheItemReader(lStream, ZDecrementOpenStreamCount, lContext);
                mOpenStreamCount++;

                return true;
            }
        }

        internal bool TryTouch(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryTouch));

            lock (mLock)
            {
                if (!mCached) throw new InvalidOperationException();

                if (mDeleted) return false;

                if (mOpenStreamCount == 0)
                {
                    try
                    {
                        Touch(lContext);
                        mChangeSequence++;
                    }
                    catch (Exception e) { lContext.TraceException("touch failure", e); }
                }

                return true;
            }
        }

        internal void TryAssignPersistentKey(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryAssignPersistentKey));

            lock (mLock)
            {
                if (!mCached || mPersistentKey == null) throw new InvalidOperationException();

                if (mDeleted || mPersistentKeyAssigned) return;
                
                try { AssignPersistentKey(mOpenStreamCount == 0, lContext); }
                catch (Exception e) { lContext.TraceException("assignpersistentkey failure", e); }

                if (mPersistentKeyAssigned) mChangeSequence++;
            }
        }

        internal bool TryDelete(int pChangeSequence, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryDelete), pChangeSequence);

            lock (mLock)
            {
                if (mDeleted)
                {
                    lContext.TraceVerbose("already deleted");
                    return true;
                }

                if (mOpenStreamCount != 0)
                {
                    lContext.TraceVerbose("open");
                    return false;
                }

                if (pChangeSequence != -1 && pChangeSequence != mChangeSequence)
                {
                    lContext.TraceVerbose("modified");
                    return false;
                }

                try { YDelete(lContext); }
                catch (Exception e) { lContext.TraceException("delete failure", e); }

                lContext.TraceVerbose("deleted");
                mDeleted = true;

                if (mCached)
                {
                    try { ItemDecached(lContext); }
                    catch (Exception e) { lContext.TraceException("itemdecached event failure", e); }
                }
            }

            return true;
        }

        internal int ChangeSequence => mChangeSequence;

        private void ZDecrementOpenStreamCount(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ZDecrementOpenStreamCount));

            lock (mLock)
            {
                if (mDeleted || --mOpenStreamCount != 0) return;

                if (!mCached)
                {
                    lContext.TraceVerbose("item closed but not cached");

                    try { YDelete(lContext); }
                    catch (Exception e) { lContext.TraceException("delete failure", e); }

                    lContext.TraceVerbose("deleted");
                    mDeleted = true;

                    return;
                }

                if (Cache.IsDisposed && !mPersistentKeyAssigned)
                {
                    lContext.TraceVerbose("item closed but cache disposed and no persistent key");

                    try { YDelete(lContext); }
                    catch (Exception e) { lContext.TraceException("delete failure", e); }

                    lContext.TraceVerbose("deleted");
                    mDeleted = true;

                    return;
                }

                try
                {
                    Touch(lContext);
                    mChangeSequence++;
                }
                catch (Exception e) { lContext.TraceException("touch failure", e); }
            }
        }

        public override string ToString() => $"{nameof(cSectionCacheItem)}({Cache},{ItemKey},{ItemSequence})";
    }
}