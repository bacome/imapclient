using System;
using System.IO;
using System.Threading;
using work.bacome.mailclient;
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

        internal bool IsExistingItem => mReadWriteStream == null;

        public bool Deleted => mDeleted;

        // for use by the derived class if it notices that an item has been deleted without its knowledge
        protected void SetDeleted(cTrace.cContext pParentContext)
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
                catch { }
            }
        }

        public cSectionCachePersistentKey PersistentKey
        {
            get
            {
                if (mPersistentKey != null) return mPersistentKey;
                if (mNonPersistentKey == null || mNonPersistentKey.UID == null) return null;
                mPersistentKey = new cSectionCachePersistentKey(mNonPersistentKey);
                return mPersistentKey;
            }
        }

        protected internal abstract object ItemKey { get; }

        protected internal abstract bool PersistentKeyAssigned { get; }

        protected abstract Stream GetReadStream(cTrace.cContext pParentContext);

        protected virtual void Touch(cTrace.cContext pParentContext) { }
        protected virtual void Delete(cTrace.cContext pParentContext) { }

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
                catch { }
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
                catch { }
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
                catch { }

                lContext.TraceVerbose("deleted");
                mDeleted = true;

                lCached = mCached;
            }

            if (lCached)
            {
                try { Cache.ItemDeleted(this, lContext); }
                catch { }
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
                    lContext.TraceError("failed to get reader:\n{0}", e);

                    if (lStream != null)
                    {
                        try { lStream.Dispose(); }
                        catch { }
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
                    catch { }
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
                    catch { }
                }

                if (!mCached || (Cache.IsClosed && (Cache.Temporary || !PersistentKeyAssigned)))
                {
                    try { Delete(lContext); }
                    catch { }
                    mDeleted = true;
                    return;
                }

                try
                {
                    Touch(lContext);
                    mChangeSequence++;
                }
                catch { }
            }

            try { Cache.ItemClosed(lContext); }
            catch { }
        }
    }
}