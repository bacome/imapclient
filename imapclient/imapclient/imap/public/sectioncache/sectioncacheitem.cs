using System;
using System.IO;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cSectionCache
    {
        public abstract partial class cItem
        {
            private readonly object mLock = new object();
            private readonly cSectionCache mCache;

            private bool mCanWrite;
            private bool mCached;
            private bool mAssignedPersistentKey;

            // incremented when something significant changes about the cache item that should stop it from being deleted if the change wasn't taken into account by the decision to delete
            private int mChangeSequence = 0;

            private int mOpenStreamCount = 0;
            private bool mDeleted = false;
            private cSectionCachePersistentKey mPersistentKey = null;
            private cSectionCacheNonPersistentKey mNonPersistentKey = null;

            protected cItem(cSectionCache pCache, bool pNewItem)
            {
                mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));

                if (pNewItem)
                {
                    mCanWrite = true;
                    mCached = false;
                    mAssignedPersistentKey = pCache.Temporary; // if the cache is temporary, then the backing store items are not renamed
                }
                else
                {
                    mCanWrite = false;
                    mCached = true;
                    mAssignedPersistentKey = true;
                }
            }

            protected abstract Stream GetReadStream(cTrace.cContext pParentContext);
            protected abstract Stream GetReadWriteStream(cTrace.cContext pParentContext);

            protected virtual void AssignPersistentKey(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                throw new NotImplementedException();
            }

            protected virtual void Touch(cTrace.cContext pParentContext) { }
            protected virtual void Delete(cTrace.cContext pParentContext) { }

            // returns an invariant value for sorting the item
            //  for a temp cache this should be the 'touch sequence'
            //  for a persistent cache this should be the unique identifier of the item (e.g. the file name)
            //
            protected internal abstract IComparable GetSortParameters();

            // called by cSectionCacheItem
            internal int ChangeSequence => mChangeSequence;

            // called by cache when generating lists of items
            internal bool Deleted => mDeleted;

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

            // called by the sectioncache when the item is added to the internal list
            internal void SetCached(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetCached), pKey);
                
                lock (mLock)
                {
                    if (mCanWrite || mCached || mDeleted) throw new InvalidOperationException();
                    mPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
                    mCached = true;
                }
            }

            // called by the sectioncache when the item is added to the internal list
            internal void SetCached(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetCached), pKey);

                lock (mLock)
                {
                    if (mCanWrite || mCached || mDeleted) throw new InvalidOperationException();
                    mNonPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
                    mCached = true;
                }
            }

            // called by the sectioncache
            internal bool TryGetReader(out cReader rReader, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(TryGetReader));

                lock (mLock)
                {
                    if (mCanWrite || !mCached) throw new InvalidOperationException();

                    if (mDeleted)
                    {
                        lContext.TraceVerbose("deleted");
                        rReader = null;
                        return false;
                    }

                    rReader = new cReader(this, lContext);
                    mOpenStreamCount++;
                    return true;
                }
            }

            // called by the sectioncache to check that the concrete implementation hasn't done something dumb
            internal bool CanWrite => mCanWrite;

            // called by the sectioncache
            internal cReaderWriter GetReaderWriter(cSectionCachePersistentKey pKey, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(GetReaderWriter), pKey);

                lock (mLock)
                {
                    if (!mCanWrite || mDeleted) throw new InvalidOperationException();
                    mCanWrite = false;
                    mOpenStreamCount++;
                    return new cReaderWriter(this, pKey, pWriteSizer, lContext);
                }
            }

            // called by the sectioncache
            internal cReaderWriter GetReaderWriter(cSectionCacheNonPersistentKey pKey, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(GetReaderWriter), pKey);

                lock (mLock)
                {
                    if (!mCanWrite || mDeleted) throw new InvalidOperationException();
                    mCanWrite = false;
                    mOpenStreamCount++;
                    return new cReaderWriter(this, pKey, pWriteSizer, lContext);
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
                    if (mCanWrite || !mCached) throw new InvalidOperationException();

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
                    if (mCanWrite || !mCached) throw new InvalidOperationException();

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
                    if (--mOpenStreamCount != 0) return;

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
                    if (ReferenceEquals(mPersistentKey, null))
                    {
                        if (ReferenceEquals(mNonPersistentKey, null)) throw new cInternalErrorException(nameof(cItem), nameof(ZDecrementOpenStreamCount));
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