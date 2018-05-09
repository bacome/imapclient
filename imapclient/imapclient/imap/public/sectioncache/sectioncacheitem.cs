using System;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cSectionCacheItem
    {
        private readonly object mLock = new object();

        internal readonly cSectionCache Cache;
        private readonly Stream mReadWriteStream;
        private bool mCached;

        // incremented when something significant changes about the cache item that should stop it from being deleted if the change wasn't taken into account by the decision to delete
        private int mChangeSequence = 0;

        private int mOpenStreamCount = 0;
        private bool mDeleted = false;
        private bool mIndexed = false;
        private cSectionCachePersistentKey mPersistentKey = null;
        private cSectionCacheNonPersistentKey mNonPersistentKey = null;

        public cSectionCacheItem(cSectionCache pCache)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            mReadWriteStream = null;
            mCached = true;
        }

        public cSectionCacheItem(cSectionCache pCache, Stream pReadWriteStream)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            mReadWriteStream = pReadWriteStream ?? throw new ArgumentNullException(nameof(pReadWriteStream));
            if (!pReadWriteStream.CanRead || !pReadWriteStream.CanSeek || !pReadWriteStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pReadWriteStream));
            mCached = false;
        }

        protected internal abstract object ItemKey { get; }
        protected abstract Stream GetReadStream(cTrace.cContext pParentContext);

        protected internal virtual bool PersistentKeyAssigned { get => false; }
        protected virtual void Touch(cTrace.cContext pParentContext) { }
        protected virtual void Delete(cTrace.cContext pParentContext) { }

        // for use by the derived class if it notices that an item has been deleted without its knowledge
        public void SetDeleted(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetDeleted));

            bool lCached;

            lock (mLock)
            {
                if (mDeleted) return;
                mDeleted = true;
                lCached = mCached;
            }

            if (lCached)
            {
                try { Cache.ItemDeleted(this, lContext); }
                catch (Exception e) { lContext.TraceException("itemdeleted event failure", e); }
            }
        }

        public bool Deleted => mDeleted;

        internal bool Indexed => mIndexed;

        internal void SetIndexed(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetIndexed));
            if (mIndexed) throw new InvalidOperationException();
            mIndexed = true;
        }

        internal cSectionCachePersistentKey PersistentKey
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

            bool lDeleted;

            lock (mLock)
            {
                if (mCached) throw new InvalidOperationException();
                mCached = true;
                mPersistentKey = pKey;
                lDeleted = mDeleted;
            }

            if (!lDeleted)
            {
                try { Cache.ItemAdded(this, lContext); }
                catch (Exception e) { lContext.TraceException("itemadded event failure", e); }
            }
        }

        internal void SetCached(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetCached), pKey);

            if (pKey == null) throw new ArgumentNullException(nameof(pKey));

            bool lDeleted;

            lock (mLock)
            {
                if (mCached) throw new InvalidOperationException();
                mCached = true;
                mNonPersistentKey = pKey;
                lDeleted = mDeleted;
            }

            if (!lDeleted)
            {
                try { Cache.ItemAdded(this, lContext); }
                catch (Exception e) { lContext.TraceException("itemadded event failure", e); }
            }
        }

        internal int ChangeSequence => mChangeSequence;

        internal bool TryDelete(int pChangeSequence, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryDelete), pChangeSequence);

            bool lCached;

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

                try { Delete(lContext); }
                catch (Exception e) { lContext.TraceException("delete failure", e); }

                lContext.TraceVerbose("deleted");
                mDeleted = true;

                lCached = mCached;
            }

            if (lCached)
            {
                try { Cache.ItemDeleted(this, lContext); }
                catch (Exception e) { lContext.TraceException("itemdeleted event failure", e); }
            }

            return true;
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
                    lStream = GetReadStream(lContext);
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

        private void ZDecrementOpenStreamCount(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ZDecrementOpenStreamCount));

            lock (mLock)
            {
                if (mDeleted || --mOpenStreamCount != 0) return;

                if (mReadWriteStream != null)
                {
                    try { mReadWriteStream.Dispose(); }
                    catch (Exception e) { lContext.TraceException("readwritestream dispose failure", e); }
                }

                if (!mCached || (Cache.IsClosed && !PersistentKeyAssigned))
                {
                    try { Delete(lContext); }
                    catch (Exception e) { lContext.TraceException("delete failure", e); }
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
    }
}