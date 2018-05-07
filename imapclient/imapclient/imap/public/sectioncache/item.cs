using System;
using System.IO;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cSectionCache
    {
        protected internal abstract partial class cItem
        {
            private readonly object mLock = new object();
            private readonly cSectionCache mCache;

            private int mOpenStreamCount;
            private bool mCached;
            private long mLength;
            private bool mAssignedPersistentKey;

            // incremented when something significant changes about the cache item that should stop it from being deleted if the change wasn't taken into account by the decision to delete
            private int mChangeSequence = 0;

            private bool mDeleted = false;
            private cSectionCachePersistentKey mPersistentKey = null;
            private cSectionCacheNonPersistentKey mNonPersistentKey = null;

            protected cItem(cSectionCache pCache, long pLength)
            {
                mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));

                mOpenStreamCount = 0;
                mCached = true;
                mLength = pLength;
                mAssignedPersistentKey = true;
            }

            protected cItem(cSectionCache pCache)
            {
                mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));

                mOpenStreamCount = 1;
                mCached = false;
                mLength = -1;
                mAssignedPersistentKey = pCache.Temporary; // if the cache is temporary, then we don't call assignpersistentkey
            }

            public long Length
            {
                get
                {
                    if (!mCached) throw new InvalidOperationException();
                    return mLength;
                }
            }

            public bool Deleted => mDeleted;

            // called by cache when generating lists of items
            protected internal abstract object GetItemKey();

            protected abstract Stream GetReadStream(cTrace.cContext pParentContext);

            protected virtual void AssignPersistentKey(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                throw new NotImplementedException();
            }

            protected virtual void Touch(cTrace.cContext pParentContext) { }
            protected virtual void Delete(cTrace.cContext pParentContext) { }

            // called by cSectionCacheItem
            internal int ChangeSequence => mChangeSequence;

            // called by cache trimming via cSectionCacheItem and when a cache accessor closes
            internal bool TryDelete(int pChangeSequence, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(TryDelete), pChangeSequence);

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
                }

                if (mCached) mCache.ItemDeleted(this, lContext);

                return true;
            }

            // called by the sectioncache to check that the implementation hasn't done something dumb
            internal bool Cached => mCached;

            // called by the sectioncache when the item is added to the internal list
            internal void SetCached(long pLength, cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetCached), pKey, pLength);

                if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                if (pKey == null) throw new ArgumentNullException(nameof(pKey));
                
                lock (mLock)
                {
                    if (mCached || mDeleted) throw new InvalidOperationException();
                    mCached = true;
                    mLength = pLength;
                    mPersistentKey = pKey;
                }
            }

            // called by the sectioncache when the item is added to the internal list
            internal void SetCached(long pLength, cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetCached), pKey);

                if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                if (pKey == null) throw new ArgumentNullException(nameof(pKey));

                lock (mLock)
                {
                    if (mCached || mDeleted) throw new InvalidOperationException();
                    mCached = true;
                    mLength = pLength;
                    mNonPersistentKey = pKey;
                }
            }

            // called by the sectioncache
            internal bool TryGetReader(out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(TryGetReader));

                lock (mLock)
                {
                    if (!mCached) throw new InvalidOperationException();

                    if (mDeleted)
                    {
                        lContext.TraceVerbose("deleted");
                        rReader = null;
                        return false;
                    }

                    ;?; // dispose protection

                    Stream lStream;

                    try
                    {
                        lStream = GetReadStream(lContext);

                        if (LSTR)


                    }
                    catch
                    {
                        lContext.TraceError("failed to get readerwriter:\n{0}", e);
                        lStream = null;
                    }

                    ;????????;

                    if (lStream == null)
                    {
                        lContext.TraceVerbose("getreadstream failed: assuming deleted");
                        mDeleted = true;
                        rReader = null;
                        return false;
                    }

                    if (!lStream.CanRead || !lStream.CanSeek) throw new cUnexpectedSectionCacheActionException(lContext);

                    mOpenStreamCount++;
                    rReader = new cSectionCacheItemReader(lStream, ZDecrementOpenStreamCount, lContext);
                    return true;
                }
            }

            // called by the sectioncache to check that the concrete implementation hasn't done something dumb, in closedown, and when generating lists of items
            internal bool AssignedPersistentKey => mAssignedPersistentKey;

            // called by the sectioncache 
            internal void TryAssignPersistentKey(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(TryAssignPersistentKey), pKey);

                lock (mLock)
                {
                    if (!mCached) throw new InvalidOperationException();

                    if (!mDeleted && !mAssignedPersistentKey && mOpenStreamCount == 0)
                    {
                        try
                        {
                            AssignPersistentKey(pKey, lContext);
                            mAssignedPersistentKey = true;
                            mChangeSequence++;
                        }
                        catch { }
                    }
                }
            }

            // called by the sectioncache 
            internal bool TryTouch(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(TryTouch));

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

            // called by the reader and the readerwriter when they are disposed
            private void ZDecrementOpenStreamCount(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZDecrementOpenStreamCount));

                lock (mLock)
                {
                    if (mDeleted || --mOpenStreamCount != 0) return;

                    if (!mCached || (mCache.Temporary && mCache.IsClosed))
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

                if (!mAssignedPersistentKey)
                {
                    if (mPersistentKey == null)
                    {
                        if (mNonPersistentKey == null) throw new cInternalErrorException(nameof(cItem), nameof(ZDecrementOpenStreamCount));
                        if (mNonPersistentKey.UID != null) mCache.ZTryAssignPersistentKey(new cSectionCachePersistentKey(mNonPersistentKey), this, lContext);
                    }
                    else mCache.ZTryAssignPersistentKey(mPersistentKey, this, lContext);
                }

                try { mCache.ItemClosed(lContext); }
                catch { }
            }
        }
    }
}