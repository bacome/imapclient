using System;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cSectionCacheItem
    {
        private readonly object mLock = new object();

        public readonly cSectionCache Cache;
        public readonly int ItemSequence;
        public readonly string ItemKey;
        private readonly Stream mReadWriteStream;
        private bool mCanGetReaderWriter;
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
            ItemSequence = pCache.GetItemSequence();
            ItemKey = pItemKey ?? throw new ArgumentNullException(nameof(pItemKey));
            mReadWriteStream = null;
            mCanGetReaderWriter = false;
            mCached = true;
            mPersistentKeyAssigned = true;
        }

        public cSectionCacheItem(cSectionCache pCache, string pItemKey, Stream pReadWriteStream)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            ItemSequence = pCache.GetItemSequence();
            ItemKey = pItemKey ?? throw new ArgumentNullException(nameof(pItemKey));
            mReadWriteStream = pReadWriteStream ?? throw new ArgumentNullException(nameof(pReadWriteStream));
            if (!pReadWriteStream.CanRead || !pReadWriteStream.CanSeek || !pReadWriteStream.CanWrite || pReadWriteStream.Position != 0) throw new ArgumentOutOfRangeException(nameof(pReadWriteStream));
            mCanGetReaderWriter = true;
            mCached = false;
            mPersistentKeyAssigned = false;
        }

        protected abstract Stream YGetReadStream(cTrace.cContext pParentContext);
        protected abstract void YDelete(cTrace.cContext pParentContext);

        public int ChangeSequence => mChangeSequence;

        // the persistent key should be set here if it can be set while the item is open
        protected virtual void ItemCached(long pLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ItemCached), pLength);
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

        /* TODO: remove
        // for use if it is noticed that the item is already deleted, or by the derived classes if they delete (probably by renaming) the item
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
            }
        } */


        // for use by the derived class when it sets the persistent key
        protected void SetPersistentKeyAssigned(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetPersistentKeyAssigned));
            if (!mCached) throw new InvalidOperationException();
            if (mPersistentKeyAssigned) return;
            mPersistentKeyAssigned = true;
        }

        protected internal bool PersistentKeyAssigned => mPersistentKeyAssigned;

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

        protected internal bool TryDelete(int pChangeSequence, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryDelete), pChangeSequence);

            if (mCanGetReaderWriter) throw new InvalidOperationException();

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
            }

            return true;
        }

        internal bool Deleted => mDeleted;

        internal bool CanGetReaderWriter => mCanGetReaderWriter;

        internal cSectionCacheItemReaderWriter GetReaderWriter(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(GetReaderWriter));

            cSectionCacheItemReaderWriter lReaderWriter;

            lock (mLock)
            {
                if (!mCanGetReaderWriter) throw new InvalidOperationException();
                mCanGetReaderWriter = false;
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

            if (mCanGetReaderWriter) throw new InvalidOperationException();

            lock (mLock)
            {
                if (mCached) throw new InvalidOperationException();

                mCached = true;
                mPersistentKey = pKey;

                if (!mDeleted)
                {
                    try { ItemCached(mReadWriteStream.Length, lContext); }
                    catch (Exception e) { lContext.TraceException("itemcached event failure", e); }
                }
            }
        }

        internal void SetCached(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetCached), pKey);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));

            if (mCanGetReaderWriter) throw new InvalidOperationException();

            lock (mLock)
            {
                if (mCached) throw new InvalidOperationException();

                mCached = true;
                mNonPersistentKey = pKey;

                if (!mDeleted)
                {
                    try { ItemCached(mReadWriteStream.Length, lContext); }
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

            if (mCanGetReaderWriter) throw new InvalidOperationException();

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

                try { lStream = YGetReadStream(lContext); }
                catch (Exception e) { lContext.TraceException("ygetreadstream failure", e); }

                if (lStream == null)
                {
                    mDeleted = true;
                    rReader = null;
                    return false;
                }

                if (!lStream.CanRead || !lStream.CanSeek)
                {
                    lStream.Dispose();
                    throw new cUnexpectedSectionCacheActionException(lContext);
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

                if (mOpenStreamCount != 0) return true;

                try
                {
                    Touch(lContext);
                    mChangeSequence++;
                    return true;
                }
                catch (Exception e)
                {
                    lContext.TraceException("touch failure", e);
                    mDeleted = true;
                    return false;
                }
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
                catch (Exception e)
                {
                    lContext.TraceException("touch failure", e);
                    mDeleted = true;
                }
            }
        }

        public override string ToString() => $"{nameof(cSectionCacheItem)}({Cache},{ItemKey})";
    }
}