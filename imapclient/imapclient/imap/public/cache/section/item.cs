﻿using System;
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
        public readonly object ItemId;
        private readonly Stream mReadWriteStream;
        private long mLength;
        private bool mCanGetReaderWriter;
        private bool mCached;
        private eSectionCachePersistState mPersistState;

        // incremented when something significant changes about the cache item that should stop it from being deleted if the change wasn't taken into account by the decision to delete
        private int mChangeSequence = 0;

        private int mOpenStreamCount = 0;
        private bool mDeleted = false;
        private bool mToBeDeleted = false;
        private bool mIndexed = false;

        // these are only set for items generated internally, not for items generated by the concrete class
        private cSectionId mSectionId = null;
        private cSectionHandle mSectionHandle = null;

        public cSectionCacheItem(cSectionCache pCache, object pItemId, long pLength)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            ItemSequence = pCache.GetItemSequence();
            ItemId = pItemId ?? throw new ArgumentNullException(nameof(pItemId));
            mReadWriteStream = null;
            if (mLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            mLength = pLength;
            mCanGetReaderWriter = false;
            mCached = true;
            mPersistState = eSectionCachePersistState.persisted;
        }

        public cSectionCacheItem(cSectionCache pCache, object pItemId, Stream pReadWriteStream, uint pUIDValidity, bool pUIDNotSticky)
        {
            Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            ItemSequence = pCache.GetItemSequence();
            ItemId = pItemId ?? throw new ArgumentNullException(nameof(pItemId));
            mReadWriteStream = pReadWriteStream ?? throw new ArgumentNullException(nameof(pReadWriteStream));
            if (!pReadWriteStream.CanRead || !pReadWriteStream.CanSeek || !pReadWriteStream.CanWrite || pReadWriteStream.Position != 0) throw new ArgumentOutOfRangeException(nameof(pReadWriteStream));
            mLength = -1;
            mCanGetReaderWriter = true;
            mCached = false;

            if (pUIDValidity == 0 || pUIDNotSticky) mPersistState = eSectionCachePersistState.cannotbepersisted;
            else mPersistState = eSectionCachePersistState.notpersisted;
        }

        public cSectionCacheItem(cSectionCacheItem pItemToCopy, object pItemId)
        {
            if (pItemToCopy == null) throw new ArgumentNullException(nameof(pItemToCopy));
            if (pItemToCopy.PersistState == eSectionCachePersistState.persisted) throw new ArgumentOutOfRangeException(nameof(pItemToCopy));
            Cache = pItemToCopy.Cache;
            ItemSequence = pItemToCopy.Cache.GetItemSequence();
            ItemId = pItemId ?? throw new ArgumentNullException(nameof(pItemId));
            mReadWriteStream = null;
            mLength = pItemToCopy.Length;
            mCanGetReaderWriter = false;
            mCached = true;
            mPersistState = pItemToCopy.mPersistState;
        }

        internal void SetSectionId(cSectionId pSectionId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetSectionId), pSectionId);
            if (mCached || mSectionId != null || mSectionHandle != null) throw new InvalidOperationException();
            mSectionId = pSectionId;
        }

        internal void SetSectionHandle(cSectionHandle pSectionHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(SetSectionHandle), nameof(SetSectionId), pSectionHandle);
            if (mCached || mSectionId != null || mSectionHandle != null) throw new InvalidOperationException();
            mSectionHandle = pSectionHandle;
        }

        protected abstract Stream YGetReadStream(cTrace.cContext pParentContext);
        protected abstract void YDelete(cTrace.cContext pParentContext);

        public int ChangeSequence => mChangeSequence;

        protected virtual eItemState YTouch(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(YTouch));
            return eItemState.exists;
        }

        protected virtual eSectionCachePersistState YTryPersist(bool pItemClosed, cSectionId pSectionId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(YTryPersist), pItemClosed, pSectionId);
            return eSectionCachePersistState.cannotbepersisted;
        }

        protected virtual cSectionCacheItem YCopy(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(YCopy));
            return null;
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

        internal long Length => mLength;
        internal bool CanGetReaderWriter => mCanGetReaderWriter;
        internal bool Cached => mCached;
        internal eSectionCachePersistState PersistState => mPersistState;
        internal bool Deleted => mDeleted;
        internal bool ToBeDeleted => mToBeDeleted;
        internal bool Indexed => mIndexed;

        internal cSectionId SectionId
        {
            get
            {
                if (mSectionId != null) return mSectionId;
                if (mSectionHandle == null || mSectionHandle.MessageHandle.UID == null) return null;
                mSectionId = new cSectionId(new cMessageUID(mSectionHandle.MessageHandle.MessageCache.MailboxHandle.MailboxId, mSectionHandle.MessageHandle.UID), mSectionHandle.Section, mSectionHandle.Decoding);
                return mSectionId;
            }
        }

        internal cSectionHandle SectionHandle => mSectionHandle;

        internal bool TryCopy(cSectionId pSectionId, out cSectionCacheItem rNewItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryCopy));

            lock (mLock)
            {
                if (!mCached) throw new InvalidOperationException();
                if (mPersistState == eSectionCachePersistState.persisted) { rNewItem = null; return false; }

                try { rNewItem = YCopy(lContext); }
                catch (Exception e)
                {
                    lContext.TraceException(e);
                    rNewItem = null;
                    return false;
                }

                if (rNewItem == null || rNewItem.Cache != Cache || rNewItem.mReadWriteStream != null || rNewItem.mLength != mLength || !rNewItem.mCached || rNewItem.mPersistState != mPersistState || rNewItem.Deleted || rNewItem.ToBeDeleted) throw new cUnexpectedSectionCacheActionException(lContext);
                rNewItem.mSectionId = pSectionId;

                return true;
            }
        }

        internal bool TryRename(cSectionId pSectionId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryRename), pSectionId);

            lock (mLock)
            {
                if (!mCached) throw new InvalidOperationException();
                if (mPersistState == eSectionCachePersistState.persisted) return false;
                mSectionId = pSectionId;
                return true;
            }
        }

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

        internal void SetCached(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(SetCached));

            if (mCanGetReaderWriter) throw new InvalidOperationException();

            lock (mLock)
            {
                if (mCached) throw new InvalidOperationException();

                mLength = mReadWriteStream.Length;
                mCached = true;

                ZTryPersist(lContext);
            }
        }

        internal void TryPersist(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryPersist));

            lock (mLock)
            {
                if (!mCached) throw new InvalidOperationException();
                ZTryPersist(lContext);
            }
        }

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
                return ZTryTouch(lContext);
            }
        }

        private void ZDecrementOpenStreamCount(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ZDecrementOpenStreamCount));

            lock (mLock)
            {
                if (mDeleted || --mOpenStreamCount != 0) return;

                if (!mCached || mToBeDeleted || (Cache.IsDisposed && mPersistState != eSectionCachePersistState.persisted))
                {
                    lContext.TraceVerbose("item closed but either; not cached or, marked as to-be-deleted or, cache is disposed and sectionid hasn't been recorded");

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

                ZTryPersist(lContext);
                ZTryTouch(lContext);
            }
        }

        private void ZTryPersist(cTrace.cContext pParentContext)
        {
            // must be called inside the lock

            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ZTryPersist));

            if (mDeleted || mToBeDeleted || mPersistState != eSectionCachePersistState.notpersisted) return;

            if (mSectionHandle != null && mSectionHandle.MessageHandle.Expunged)
            {
                lContext.TraceVerbose("marking as cannotberecorded because it is expunged");
                mPersistState = eSectionCachePersistState.cannotbepersisted;
                return;
            }

            if (mSectionId == null)
            {
                if (mSectionHandle == null) throw new InvalidOperationException();
                if (mSectionHandle.MessageHandle.UID == null) return;
                mSectionId = new cSectionId(new cMessageUID(mSectionHandle.MessageHandle.MessageCache.MailboxHandle.MailboxId, mSectionHandle.MessageHandle.UID), mSectionHandle.Section, mSectionHandle.Decoding);
            }

            try
            {
                mPersistState = YTryPersist(mOpenStreamCount == 0, SectionId, lContext);
                if (mPersistState == eSectionCachePersistState.persisted) mChangeSequence++;
            }
            catch (Exception e)
            {
                lContext.TraceException("marking as cannotberecorded because of tryrecordsectionid failure", e);
                mPersistState = eSectionCachePersistState.cannotbepersisted;
            }
        }

        private bool ZTryTouch(cTrace.cContext pParentContext)
        {
            // must be called inside the lock

            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(ZTryTouch));

            try
            {
                if (YTouch(lContext) == eItemState.exists)
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

        public override string ToString() => $"{nameof(cSectionCacheItem)}({Cache},{ItemId})";
    }
}