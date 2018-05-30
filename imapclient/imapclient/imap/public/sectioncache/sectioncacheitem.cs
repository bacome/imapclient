using System;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cSectionCacheItem
    {
        protected enum eItemState { deleted, exists }

        private readonly object mLock = new object();

        public readonly cSectionCache Cache;
        public readonly int ItemSequence;
        public readonly string ItemKey;
        private readonly Stream mReadWriteStream;
        private long mLength;
        private bool mCanGetReaderWriter;
        private bool mCached;
        private bool mPersistentKeyAssigned;

        // incremented when something significant changes about the cache item that should stop it from being deleted if the change wasn't taken into account by the decision to delete
        private int mChangeSequence = 0;

        private int mOpenStreamCount = 0;
        private bool mDeleted = false;
        private bool mToBeDeleted = false;
        private bool mIndexed = false;
        private cSectionCachePersistentKey mPersistentKey = null;
        private cSectionCacheNonPersistentKey mNonPersistentKey = null;

        public cSectionCacheItem(cSectionCache pCache, string pItemKey, long pLength)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            ItemSequence = pCache.GetItemSequence();
            ItemKey = pItemKey ?? throw new ArgumentNullException(nameof(pItemKey));
            mReadWriteStream = null;
            if (mLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            mLength = pLength;
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
            mLength = -1;
            mCanGetReaderWriter = true;
            mCached = false;
            mPersistentKeyAssigned = false;
        }

        protected abstract Stream YGetReadStream(cTrace.cContext pParentContext);
        protected abstract void YDelete(cTrace.cContext pParentContext);

        public int ChangeSequence => mChangeSequence;

        // the persistent key should be set here if it can be set while the item is open
        protected virtual void ItemCached(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ItemCached));
        }

        // the persistent key should be set here if it can only be set while the item is closed
        protected virtual eItemState Touch(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(Touch));
            return eItemState.exists;
        }

        // called periodically and in cache closedown if the persistent key is not assigned but one is available
        protected virtual void AssignPersistentKey(bool pItemClosed, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(AssignPersistentKey), pItemClosed);
        }

        // for use by the derived class when it sets the persistent key
        protected void SetPersistentKeyAssigned(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetPersistentKeyAssigned));
            if (!mCached) throw new InvalidOperationException();
            mPersistentKeyAssigned = true;
        }

        public bool PersistentKeyAssigned => mPersistentKeyAssigned;

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

                if (mToBeDeleted)
                {
                    lContext.TraceVerbose("already scheduled for deletion");
                    return true;
                }

                if (mOpenStreamCount != 0)
                {
                    if (pChangeSequence == -2)
                    {
                        lContext.TraceVerbose("open, marking as todelete");
                        mToBeDeleted = true;
                    }
                    else lContext.TraceVerbose("open, not deleting");

                    return false;
                }

                if (pChangeSequence >= 0 && pChangeSequence != mChangeSequence)
                {
                    lContext.TraceVerbose("modified, not deleting");
                    return false;
                }

                try
                {
                    YDelete(lContext);
                    lContext.TraceVerbose("deleted");
                }
                catch (Exception e)
                {
                    lContext.TraceException("delete failure, marked as deleted", e);
                }

                mDeleted = true;
            }

            return true;
        }

        internal bool Deleted => mDeleted;

        internal bool ToBeDeleted => mToBeDeleted;

        internal long Length => mLength;

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

                mLength = mReadWriteStream.Length;
                mCached = true;
                mPersistentKey = pKey;

                if (!mDeleted && !mToBeDeleted)
                {
                    try { ItemCached(lContext); }
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

                mLength = mReadWriteStream.Length;
                mCached = true;
                mNonPersistentKey = pKey;

                if (!mDeleted && !mToBeDeleted)
                {
                    try { ItemCached(lContext); }
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

                if (mToBeDeleted)
                {
                    lContext.TraceVerbose("to be deleted");
                    rReader = null;
                    return false;
                }

                Stream lStream = null;

                try { lStream = YGetReadStream(lContext); }
                catch (Exception e) { lContext.TraceException("ygetreadstream failure", e); }

                if (lStream == null || lStream.Length != mLength)
                {
                    lContext.TraceWarning("marking as deleted because no stream was returned or the stream was the wrong length");
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

                if (mDeleted || mToBeDeleted) return false;

                if (mOpenStreamCount != 0) return true;

                try
                {
                    if (Touch(lContext) == eItemState.exists)
                    {
                        lContext.TraceVerbose("touched");
                        mChangeSequence++;
                        return true;
                    }
                    else
                    {
                        lContext.TraceVerbose("deleted");
                        mDeleted = true;
                        return false;
                    }
                }
                catch (Exception e)
                {
                    lContext.TraceException("marking as deleted because of touch failure", e);
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

                if (mDeleted || mToBeDeleted || mPersistentKeyAssigned) return;

                if (mNonPersistentKey != null && mNonPersistentKey.MailboxHandle.SelectedProperties.UIDNotSticky != false) return;

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

                    try
                    {
                        YDelete(lContext);
                        lContext.TraceVerbose("deleted");
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("delete failure, marked as deleted", e);
                    }

                    mDeleted = true;

                    return;
                }

                if (mToBeDeleted)
                {
                    lContext.TraceVerbose("item closed and marked as to be deleted");

                    try
                    {
                        YDelete(lContext);
                        lContext.TraceVerbose("deleted");
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("delete failure, marked as deleted", e);
                    }

                    mDeleted = true;

                    return;
                }

                if (Cache.IsDisposed && !mPersistentKeyAssigned)
                {
                    lContext.TraceVerbose("item closed and cache disposed and no persistent key");

                    try
                    {
                        YDelete(lContext);
                        lContext.TraceVerbose("deleted");
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("delete failure, marked as deleted", e);
                    }

                    mDeleted = true;

                    return;
                }

                try
                {
                    if (Touch(lContext) == eItemState.exists)
                    {
                        lContext.TraceVerbose("touched");
                        mChangeSequence++;
                    }
                    else
                    {
                        lContext.TraceVerbose("deleted by touch");
                        mDeleted = true;
                    }
                }
                catch (Exception e)
                {
                    lContext.TraceException("marking as deleted because of touch failure", e);
                    mDeleted = true;
                }
            }
        }

        public override string ToString() => $"{nameof(cSectionCacheItem)}({Cache},{ItemKey})";
    }
}