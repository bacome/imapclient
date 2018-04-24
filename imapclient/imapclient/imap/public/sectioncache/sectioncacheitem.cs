using System;
using System.IO;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public enum eSectionCacheItemDeleteResult { alreadydeleted, notdeleted, deleted }

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
                    mAssignedPersistentKey = false;
                }
                else
                {
                    mCanWrite = false;
                    mCached = true;
                    mAssignedPersistentKey = true;
                }
            }

            protected abstract Stream ReadStream { get; }
            protected abstract Stream ReadWriteStream { get; }
            protected abstract void AssignPersistentKey(cSectionCachePersistentKey pKey);
            protected abstract void Touch();
            protected abstract void Delete();

            public int ChangeSequence => mChangeSequence;

            protected internal eSectionCacheItemDeleteResult TryDelete(int pChangeSequence, cTrace.cContext pParentContext)
            {
                lock (mLock)
                {
                    var lContext = pParentContext.NewMethod(nameof(cItem), nameof(TryDelete), pChangeSequence);

                    if (mDeleted)
                    {
                        lContext.TraceVerbose("already deleted");
                        return eSectionCacheItemDeleteResult.alreadydeleted;
                    }

                    if (mOpenStreamCount != 0)
                    {
                        lContext.TraceVerbose("open");
                        return eSectionCacheItemDeleteResult.notdeleted;
                    }

                    if (pChangeSequence != -1 && pChangeSequence != mChangeSequence)
                    {
                        lContext.TraceVerbose("modified");
                        return eSectionCacheItemDeleteResult.notdeleted;
                    }

                    try { Delete(); }
                    catch { }

                    mDeleted = true;

                    lContext.TraceVerbose("deleted");
                    return eSectionCacheItemDeleteResult.deleted;
                }
            }

            internal void SetCached(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetCached), pKey);

                lock (mLock)
                {
                    if (mCanWrite || mCached) throw new InvalidOperationException();
                    mPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
                    mCached = true;
                }
            }

            internal void SetCached(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetCached), pKey);

                lock (mLock)
                {
                    if (mCanWrite || mCached) throw new InvalidOperationException();
                    mNonPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
                    mCached = true;
                }
            }

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

            internal bool CanWrite => mCanWrite;

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

            internal bool AssignedPersistentKey => mAssignedPersistentKey;

            internal void TryAssignPersistentKey(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                lock (mLock)
                {
                    if (mCanWrite || !mCached) throw new InvalidOperationException();

                    if (!mDeleted && !mAssignedPersistentKey && mOpenStreamCount == 0)
                    {
                        try
                        {
                            AssignPersistentKey(pKey);
                            mAssignedPersistentKey = true;
                            mChangeSequence++; // significant during the close of a persistent cache (as non-pk items should be deleted)
                        }
                        catch { }
                    }
                }
            }

            internal bool TryTouch(cTrace.cContext pParentContext)
            {
                lock (mLock)
                {
                    if (mCanWrite || !mCached) throw new InvalidOperationException();

                    if (mDeleted) return false;

                    if (mOpenStreamCount == 0)
                    {
                        try
                        {
                            Touch();
                            mChangeSequence++;
                        }
                        catch { }
                    }

                    return true;
                }
            }

            private void ZDecrementOpenStreamCount(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZDecrementOpenStreamCount));

                lock (mLock)
                {
                    if (--mOpenStreamCount != 0) return;

                    if (!mCached || (mCache.Temporary && mCache.IsClosed))
                    {
                        try { Delete(); }
                        catch { }
                        mDeleted = true;
                        return;
                    }

                    try
                    {
                        Touch();
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

                try { mCache.ItemClosed(); }
                catch { }
            }
        }
    }
}