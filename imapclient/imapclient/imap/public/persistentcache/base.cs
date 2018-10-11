using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cPersistentCache
    {
        private readonly object mMailboxToSelectedCountLock = new object();
        private readonly Dictionary<cMailboxId, int> mMailboxToSelectedCount = new Dictionary<cMailboxId, int>();

        private readonly ConcurrentDictionary<cSectionHandle, iSectionCacheItem> mSectionHandleToCacheItem = new ConcurrentDictionary<cSectionHandle, iSectionCacheItem>();

        public virtual void BeforeSelect(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(BeforeSelect), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            lock (mMailboxToSelectedCountLock)
            {
                int lSelectedCount;

                if (mMailboxToSelectedCount.TryGetValue(pMailboxId, out lSelectedCount)) lSelectedCount++;
                else lSelectedCount = 1;

                if (lSelectedCount == 1) YBeforeFirstSelect(pMailboxId, lContext);

                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;
            }
        }

        protected virtual void YBeforeFirstSelect(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(YBeforeFirstSelect), pMailboxId);
        }

        public virtual void AfterUnselect(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(AfterUnselect), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            lock (mMailboxToSelectedCountLock)
            {
                if (!mMailboxToSelectedCount.TryGetValue(pMailboxId, out var lSelectedCount)) throw new cInternalErrorException(lContext);
                lSelectedCount--;
                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;

                if (lSelectedCount == 0) YAfterLastUnselect(pMailboxId, lContext);
            }
        }

        protected virtual void YAfterLastUnselect(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(YAfterLastUnselect), pMailboxId);
        }

        public abstract sUIDValidity GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext); // in the override, if you discover that you have different UIDValidities, clear the cache components with lower ones
        public abstract ulong GetHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext);

        public HashSet<cUID> GetUIDs(cMailboxUID pMailboxUID, bool pForCachedFlagsOnly, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDs), pMailboxUID, pForCachedFlagsOnly);
            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));
            var lUIDs = YGetUIDs(pMailboxUID, pForCachedFlagsOnly, lContext);
            foreach (var lUID in lUIDs) if (lUID == null || lUID.UIDValidity != pMailboxUID.UIDValidity) throw new cUnexpectedPersistentCacheActionException(lContext);
            return lUIDs;
        }

        protected abstract HashSet<cUID> YGetUIDs(cMailboxUID pMailboxUID, bool pCachedFlagsOnly, cTrace.cContext pParentContext);

        internal void MessageCacheInvalidated(iMessageCache pMessageCache, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageCacheInvalidated), pMessageCache);

            if (pMessageCache == null) throw new ArgumentNullException(nameof(pMessageCache));

            var lHandlesToRemove = new List<cSectionHandle>();
            foreach (var lSectionHandle in mSectionHandleToCacheItem.Keys) if (lSectionHandle.MessageHandle.MessageCache == pMessageCache) lHandlesToRemove.Add(lSectionHandle);
            foreach (var lSectionHandle in lHandlesToRemove) mSectionHandleToCacheItem.TryRemove(lSectionHandle, out _);
        }

        internal void MessageExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageExpunged), pMessageHandle);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));

            var lHandlesToRemove = new List<cSectionHandle>();
            foreach (var lSectionHandle in mSectionHandleToCacheItem.Keys) if (lSectionHandle.MessageHandle == pMessageHandle) lHandlesToRemove.Add(lSectionHandle);
            foreach (var lSectionHandle in lHandlesToRemove) mSectionHandleToCacheItem.TryRemove(lSectionHandle, out _);

            if (pMessageHandle.MessageUID != null)
            {
                cUID[] lUIDs = new cUID[] { pMessageHandle.MessageUID.UID };
                MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);
            }
        }

        internal void MessageHandleUIDSet(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageHandleUIDSet), pMessageHandle);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pMessageHandle.MessageUID == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));

            var lItemsToMove = new List<KeyValuePair<cSectionHandle, iSectionCacheItem>>();
            foreach (var lPair in mSectionHandleToCacheItem) if (lPair.Key.MessageHandle == pMessageHandle) lItemsToMove.Add(lPair);

            foreach (var lPair in lItemsToMove)
            {
                AddSectionCacheItem(lPair.Key.SectionId, lPair.Value, lContext);
                mSectionHandleToCacheItem.TryRemove(lPair.Key, out _);
            }
        }
    
        protected internal abstract void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext);
        protected internal abstract void SetHighestModSeq(cMailboxUID pMailboxUID, ulong pHighestModSeq, cTrace.cContext pParentContext);
        protected internal abstract void NoModSeqFlagUpdate(cMailboxUID pMailboxUID, cTrace.cContext pParentContext); // means that the cache can't be sure that it is synchronised to the highestmodseq for flags

        internal void CheckUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(CheckUIDValidity), pMailboxId, pUIDValidity);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            var lUIDValidity = GetUIDValidity(pMailboxId, lContext);
            if (lUIDValidity.IsNone) return;
            if (lUIDValidity.UIDValidity != pUIDValidity) ClearCache(pMailboxId, lContext);
        }

        protected abstract void ClearCache(cMailboxId pMailboxId, cTrace.cContext pParentContext); // including uidvalidity and highestmodseq

        private HashSet<cMailboxName> ZGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ZGetMailboxNames), pAccountId);
            var lMailboxNames = YGetMailboxNames(pAccountId, lContext);
            if (lMailboxNames == null) throw new cUnexpectedPersistentCacheActionException(lContext);
            foreach (var lSectionHandle in mSectionHandleToCacheItem.Keys) if (lSectionHandle.MessageHandle.MessageCache.MailboxHandle.MailboxId.AccountId == pAccountId) lMailboxNames.Add(lSectionHandle.MessageHandle.MessageCache.MailboxHandle.MailboxId.MailboxName);
            return lMailboxNames;
        }

        protected abstract HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext);

        protected internal virtual void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);
            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
        }

        internal void Rename(cMailboxId pMailboxId, bool pUIDsAreSticky, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Rename), pMailboxId, pUIDsAreSticky, pMailboxName);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            if (pMailboxId.MailboxName.IsInbox)
            {
                ClearCache(pMailboxId, lContext);
                return;
            }

            YRename(pMailboxId, pUIDsAreSticky, pMailboxName, lContext);
            ClearCache(pMailboxId, lContext);

            var lMailboxNames = ZGetMailboxNames(pMailboxId.AccountId, lContext);

            int lStartIndex = pMailboxId.MailboxName.GetDescendantPathPrefix().Length;

            foreach (var lMailboxName in lMailboxNames)
            {
                if (!lMailboxName.IsDescendantOf(pMailboxId.MailboxName)) continue;

                var lNewPath = pMailboxName.GetDescendantPathPrefix() + lMailboxName.Path.Substring(lStartIndex);
                var lNewMailboxName = new cMailboxName(lNewPath, pMailboxName.Delimiter);
                var lOldMailboxId = new cMailboxId(pMailboxId.AccountId, lMailboxName);

                YRename(lOldMailboxId, pUIDsAreSticky, lNewMailboxName, lContext);
                ClearCache(lOldMailboxId, lContext);
            }
        }

        protected virtual void YRename(cMailboxId pMailboxId, bool pUIDsAreSticky, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(YRename), pMailboxId, pUIDsAreSticky, pMailboxName);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
            // overrides must take account of the fact that duplicates could be created by any rename done
        }

        protected internal abstract bool TryGetHeaderCacheItem(cMessageUID pMessageUID, out iHeaderCacheItem rHeaderCacheItem, cTrace.cContext pParentContext);
        protected internal abstract bool TryGetFlagCacheItem(cMessageUID pMessageUID, out iFlagCacheItem rFlagCacheItem, cTrace.cContext pParentContext);

        internal bool TryGetSectionReader(cSectionId pSectionId, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionReader), pSectionId);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            if (!YTryGetReadStream(pSectionId, out var lStream, lContext)) { rSectionReader = null; return false; }
            return ZTryGetSectionReader(lStream, out rSectionReader, lContext);
        }

        internal bool TryGetSectionReader(cSectionHandle pSectionHandle, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionReader), pSectionHandle);

            // if the handle has a uid, try getting the uid one, then try getting the handle one, then the uid one
            //  because
            //   1) if there are two copies (one uid, one handle) then we prefer the uid one (this is the purpose of the first look for uid)
            //   2) if there is one copy it could be in the process of being moved (which means we have to check for it by uid the second time)

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));

            Stream lStream;

            if (pSectionHandle.SectionId != null && YTryGetReadStream(pSectionHandle.SectionId, out lStream, lContext)) return ZTryGetSectionReader(lStream, out rSectionReader, lContext);
            if (mSectionHandleToCacheItem.TryGetValue(pSectionHandle, out var lSectionCacheItem) && lSectionCacheItem.TryGetReadStream(out lStream, lContext)) return ZTryGetSectionReader(lStream, out rSectionReader, lContext);
            if (pSectionHandle.SectionId != null && YTryGetReadStream(pSectionHandle.SectionId, out lStream, lContext)) return ZTryGetSectionReader(lStream, out rSectionReader, lContext);

            rSectionReader = null;
            return false;
        }

        protected abstract bool YTryGetReadStream(cSectionId pSectionId, out Stream rStream, cTrace.cContext pParentContext);

        private bool ZTryGetSectionReader(Stream pStream, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ZTryGetSectionReader));

            if (pStream == null) throw new cUnexpectedPersistentCacheActionException(lContext);

            try
            {
                if (!pStream.CanRead || !pStream.CanSeek || pStream.CanWrite || pStream.Position != 0) throw new cUnexpectedPersistentCacheActionException(lContext);
                rSectionReader = new cSectionReader(pStream);
                return true;
            }
            catch
            {
                pStream.Dispose();
                throw;
            }
        }

        internal cSectionReaderWriter GetSectionReaderWriter(cSectionId pSectionId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetSectionReaderWriter));

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            YGetNewSectionCacheItem(out var lStream, out var lSectionCacheItem, lContext);

            if (lStream == null) throw new cUnexpectedPersistentCacheActionException(lContext, 1);

            try
            {
                if (lSectionCacheItem == null) throw new cUnexpectedPersistentCacheActionException(lContext, 2);
                var lAdder = new cSectionIdAdder(this, pSectionId, lSectionCacheItem);
                return new cSectionReaderWriter(lStream, lAdder);
            }
            catch
            {
                lStream.Dispose();
                throw;
            }
        }

        internal cSectionReaderWriter GetSectionReaderWriter(cSectionHandle pSectionHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetSectionReaderWriter));

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));

            YGetNewSectionCacheItem(out var lStream, out var lSectionCacheItem, lContext);

            if (lStream == null) throw new cUnexpectedPersistentCacheActionException(lContext, 1);

            try
            {
                if (lSectionCacheItem == null) throw new cUnexpectedPersistentCacheActionException(lContext, 2);
                var lAdder = new cSectionHandleAdder(this, pSectionHandle, lSectionCacheItem);
                return new cSectionReaderWriter(lStream, lAdder);
            }
            catch
            {
                lStream.Dispose();
                throw;
            }
        }

        protected abstract void YGetNewSectionCacheItem(out Stream rStream, out iSectionCacheItem rSectionCacheItem, cTrace.cContext pParentContext);

        private void ZAddSectionCacheItem(cSectionHandle pSectionHandle, iSectionCacheItem pSectionCacheItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ZAddSectionCacheItem), pSectionHandle, pSectionCacheItem);

            if (mSectionHandleToCacheItem.TryGetValue(pSectionHandle, out var lSectionCacheItem))
            {
                if (lSectionCacheItem.CanGetReadStream(lContext)) return; // we have a copy of this data already
                if (!mSectionHandleToCacheItem.TryUpdate(pSectionHandle, pSectionCacheItem, lSectionCacheItem)) return; // someone else replaced it before we got a chance to
            }
            else
            {
                if (!mSectionHandleToCacheItem.TryAdd(pSectionHandle, pSectionCacheItem)) return; // someone else added it before we got a chance to
            }

            pSectionCacheItem.SetAdded(lContext); // let the cache know that the item has been added - the idea is that if it hasn't been added and it gets closed, then it can be deleted immediately

            // now see if anything important happened while the above was going on that we missed

            if (pSectionHandle.SectionId != null) MessageHandleUIDSet(pSectionHandle.MessageHandle, lContext);
            if (pSectionHandle.MessageHandle.Expunged) MessageExpunged(pSectionHandle.MessageHandle, lContext);
            if (pSectionHandle.MessageHandle.MessageCache.IsInvalid) MessageCacheInvalidated(pSectionHandle.MessageHandle.MessageCache, lContext);
        }

        protected abstract void AddSectionCacheItem(cSectionId pSectionId, iSectionCacheItem pSectionCacheItem, cTrace.cContext pParentContext);

        internal void Reconcile(cMailboxId pMailboxId, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Reconcile), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));

            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            var lMailboxNames = ZGetMailboxNames(pMailboxId.AccountId, lContext);

            foreach (var lMailboxName in lMailboxNames)
            {
                if (lMailboxName.IsChildOf(pMailboxId.MailboxName))
                {
                    if (!lSelectableChildMailboxNames.Contains(lMailboxName)) ClearCache(new cMailboxId(pMailboxId.AccountId, lMailboxName), lContext);
                }
                else if (lMailboxName.IsDescendantOf(pMailboxId.MailboxName))
                {
                    var lChildMailboxName = lMailboxName.GetLineageMemberThatIsChildOf(pMailboxId.MailboxName);
                    if (!lExistentChildMailboxNames.Contains(lChildMailboxName)) ClearCache(new cMailboxId(pMailboxId.AccountId, lMailboxName), lContext);
                }
            }
        }

        internal void Reconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Reconcile), pAccountId, pPrefix, pNotPrefixedWith);

            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pNotPrefixedWith == null) throw new ArgumentNullException(nameof(pNotPrefixedWith));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));

            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            var lMailboxNames = ZGetMailboxNames(pAccountId, lContext);

            foreach (var lMailboxName in lMailboxNames)
            {
                if (lMailboxName.IsFirstLineageMemberPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    if (!lSelectableChildMailboxNames.Contains(lMailboxName)) ClearCache(new cMailboxId(pAccountId, lMailboxName), lContext);
                }
                else if (lMailboxName.IsPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    var lFirstMailboxName = lMailboxName.GetFirstLineageMemberPrefixedWith(pPrefix);
                    if (!lExistentChildMailboxNames.Contains(lFirstMailboxName)) ClearCache(new cMailboxId(pAccountId, lMailboxName), lContext);
                }
            }
        }

        private void ZReconcile(IEnumerable<iMailboxHandle> pMailboxHandles, out HashSet<cMailboxName> rExistentMailboxNames, out HashSet<cMailboxName> rSelectableMailboxNames)
        {
            rExistentMailboxNames = new HashSet<cMailboxName>();
            rSelectableMailboxNames = new HashSet<cMailboxName>();

            foreach (var lMailboxHandle in pMailboxHandles)
            {
                if (lMailboxHandle.Exists != true) continue;
                rExistentMailboxNames.Add(lMailboxHandle.MailboxName);
                if (lMailboxHandle.ListFlags?.CanSelect != true) continue;
                rSelectableMailboxNames.Add(lMailboxHandle.MailboxName);
            }
        }

        private class cSectionHandleAdder : iSectionAdder
        {
            private readonly cPersistentCache mPersistentCache;
            private readonly cSectionHandle mSectionHandle;
            private readonly iSectionCacheItem mSectionCacheItem;

            public cSectionHandleAdder(cPersistentCache pPersistentCache, cSectionHandle pSectionHandle, iSectionCacheItem pSectionCacheItem)
            {
                mPersistentCache = pPersistentCache ?? throw new ArgumentNullException(nameof(pPersistentCache));
                mSectionHandle = pSectionHandle ?? throw new ArgumentNullException(nameof(pSectionHandle));
                mSectionCacheItem = pSectionCacheItem ?? throw new ArgumentNullException(nameof(pSectionCacheItem));
            }

            public void Add(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSectionHandleAdder), nameof(Add));
                mPersistentCache.ZAddSectionCacheItem(mSectionHandle, mSectionCacheItem, lContext);
            }
        }

        private class cSectionIdAdder : iSectionAdder
        {
            private readonly cPersistentCache mPersistentCache;
            private readonly cSectionId mSectionId;
            private readonly iSectionCacheItem mSectionCacheItem;

            public cSectionIdAdder(cPersistentCache pPersistentCache, cSectionId pSectionId, iSectionCacheItem pSectionCacheItem)
            {
                mPersistentCache = pPersistentCache ?? throw new ArgumentNullException(nameof(pPersistentCache));
                mSectionId = pSectionId ?? throw new ArgumentNullException(nameof(pSectionId));
                mSectionCacheItem = pSectionCacheItem ?? throw new ArgumentNullException(nameof(pSectionCacheItem));
            }

            public void Add(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSectionHandleAdder), nameof(Add));
                mPersistentCache.AddSectionCacheItem(mSectionId, mSectionCacheItem, lContext);
            }
        }
    }
}
