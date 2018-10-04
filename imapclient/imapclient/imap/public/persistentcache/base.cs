﻿using System;
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

        private readonly ConcurrentDictionary<cSectionHandle, iPersistentSectionCacheItem> mSectionHandleToCacheItem = new ConcurrentDictionary<cSectionHandle, iPersistentSectionCacheItem>();

        public virtual void Open(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Open), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            lock (mMailboxToSelectedCountLock)
            {
                int lSelectedCount;

                if (mMailboxToSelectedCount.TryGetValue(pMailboxId, out lSelectedCount)) lSelectedCount++;
                else lSelectedCount = 1;

                if (lSelectedCount == 1) YOpen(pMailboxId, lContext);

                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;
            }
        }

        protected virtual void YOpen(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(YOpen), pMailboxId);
        }

        public virtual void Close(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Close), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            lock (mMailboxToSelectedCountLock)
            {
                if (!mMailboxToSelectedCount.TryGetValue(pMailboxId, out var lSelectedCount)) throw new cInternalErrorException(lContext);
                lSelectedCount--;
                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;

                if (lSelectedCount == 0) YClose(pMailboxId, lContext);
            }
        }

        protected virtual void YClose(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(YClose), pMailboxId);
        }

        public abstract uint GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext);
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

            if (pMessageHandle.UID != null)
            {
                cUID[] lUIDs = new cUID[] { pMessageHandle.UID };
                MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);
            }
        }

        internal void MessageHandleUIDSet(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageHandleUIDSet), pMessageHandle);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pMessageHandle.messageUID == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));

            var lItemsToMove = new List<KeyValuePair<cSectionHandle, iPersistentSectionCacheItem>>();
            foreach (var lPair in mSectionHandleToCacheItem) if (lPair.Key.MessageHandle == pMessageHandle) lItemsToMove.Add(lPair);

            foreach (var lPair in lItemsToMove)
            {
                AddSectionCacheItem(lPair.Key.SectionId, lPair.Value, lContext);
                mSectionHandleToCacheItem.TryRemove(lPair.Key, out _);
            }
        }

        protected internal abstract void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext);
        protected internal abstract void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);
        protected internal abstract void SetHighestModSeq(cMailboxUID pMailboxUID, ulong pHighestModSeq, cTrace.cContext pParentContext);
        protected internal abstract void ClearHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext); // for flags, from now until re-opened

        internal void ClearCache(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ClearCache), pMailboxId);

            var lHandlesToRemove = new List<cSectionHandle>();
            foreach (var lSectionHandle in mSectionHandleToCacheItem.Keys) if (lSectionHandle.MessageHandle.MessageCache.MailboxHandle.MailboxId == pMailboxId) lHandlesToRemove.Add(lSectionHandle);
            foreach (var lSectionHandle in lHandlesToRemove) mSectionHandleToCacheItem.TryRemove(lSectionHandle, out _);

            YClearCache(pMailboxId, lContext);
        }

        protected abstract void YClearCache(cMailboxId pMailboxId, cTrace.cContext pParentContext); // including uidvalidity and highestmodseq

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

        internal void Rename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Rename), pMailboxId, pMailboxName);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            if (pMailboxId.MailboxName.IsInbox)
            {
                ClearCache(pMailboxId, lContext);
                return;
            }

            YRename(pMailboxId, pMailboxName, lContext);
            ClearCache(pMailboxId, lContext);

            var lMailboxNames = ZGetMailboxNames(pMailboxId.AccountId, lContext);

            int lStartIndex = pMailboxId.MailboxName.GetDescendantPathPrefix().Length;

            foreach (var lMailboxName in lMailboxNames)
            {
                if (!lMailboxName.IsDescendantOf(pMailboxId.MailboxName)) continue;

                var lNewPath = pMailboxName.GetDescendantPathPrefix() + lMailboxName.Path.Substring(lStartIndex);
                var lNewMailboxName = new cMailboxName(lNewPath, pMailboxName.Delimiter);
                var lOldMailboxId = new cMailboxId(pMailboxId.AccountId, lMailboxName);

                YRename(lOldMailboxId, lNewMailboxName, lContext);
                ClearCache(lOldMailboxId, lContext);
            }
        }

        protected virtual void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(YRename), pMailboxId, pMailboxName);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
            // overrides must take account of the fact that duplicates could be created by any rename done
        }

        protected internal abstract iPersistentHeaderCacheItem GetHeaderCacheItem(cMessageUID pMessageUID, cTrace.cContext pParentContext);
        protected internal abstract iPersistentFlagCacheItem GetFlagCacheItem(cMessageUID pMessageUID, cTrace.cContext pParentContext);

        internal bool TryGetSectionReader(cSectionId pSectionId, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionReader), pSectionId);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            if (!YTryGetReadStream(pSectionId, out var lStream, lContext)) { rSectionReader = null; return false; }
            if (lStream == null) throw new cUnexpectedPersistentCacheActionException(lContext);
            return ZTryGetSectionReader(lStream, out rSectionReader, lContext);
        }

        protected abstract bool YTryGetReadStream(cSectionId pSectionId, out Stream rStream, cTrace.cContext pParentContext);

        internal bool TryGetSectionReader(cSectionHandle pSectionHandle, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionReader), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));

            Stream lStream;

            if (!mSectionHandleToCacheItem.TryGetValue(pSectionHandle, out var lSectionCacheItem) || !lSectionCacheItem.TryGetReadStream(out lStream, lContext))
            { 
                if (pSectionHandle.SectionId == null || !YTryGetReadStream(pSectionHandle.SectionId, out lStream, lContext))
                {
                    rSectionReader = null;
                    return false;
                }
            }

            if (lStream == null) throw new cUnexpectedPersistentCacheActionException(lContext);

            return ZTryGetSectionReader(lStream, out rSectionReader, lContext);
        }

        private bool ZTryGetSectionReader(Stream pStream, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ZTryGetSectionReader));

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

        protected abstract void YGetNewSectionCacheItem(out Stream rStream, out iPersistentSectionCacheItem rSectionCacheItem, cTrace.cContext pParentContext);

        private void ZAddSectionCacheItem(cSectionHandle pSectionHandle, iPersistentSectionCacheItem pSectionCacheItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ZAddSectionCacheItem), pSectionHandle, pSectionCacheItem);

            if (mSectionHandleToCacheItem.TryGetValue(pSectionHandle, out var lSectionCacheItem))
            {
                if (!lSectionCacheItem.CanGetReadStream(lContext) && mSectionHandleToCacheItem.TryUpdate(pSectionHandle, pSectionCacheItem, lSectionCacheItem)) pSectionCacheItem.SetAdded(lContext);
            }
            else if (mSectionHandleToCacheItem.TryAdd(pSectionHandle, pSectionCacheItem)) pSectionCacheItem.SetAdded(lContext);

            if (pSectionHandle.SectionId != null) MessageHandleUIDSet(pSectionHandle.MessageHandle, lContext);
            if (pSectionHandle.MessageHandle.Expunged) MessageExpunged(pSectionHandle.MessageHandle, lContext);
            if (pSectionHandle.MessageHandle.MessageCache.IsInvalid) MessageCacheInvalidated(pSectionHandle.MessageHandle.MessageCache, lContext);
        }

        protected abstract void AddSectionCacheItem(cSectionId pSectionId, iPersistentSectionCacheItem pSectionCacheItem, cTrace.cContext pParentContext);

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

        /* does not belong here: the vanished responses have to be converted to UIDs for normal processing
        internal bool Vanished(cMailboxId pMailboxId, uint pUIDValidity, cSequenceSet pKnownUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Vanished), pMailboxId, pUIDValidity, pKnownUIDs);
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            if (pKnownUIDs == null) throw new ArgumentNullException(nameof(pKnownUIDs));
            if (!cUIntList.TryConstruct(pKnownUIDs, -1, true, out var lUInts)) return false;
            var lUIDs = new List<cUID>(from lUID in lUInts select new cUID(pUIDValidity, lUID));
            MessagesExpunged(pMailboxId, lUIDs, lContext);
            return true;
        } */


        private class cSectionHandleAdder : iSectionAdder
        {
            private readonly cPersistentCache mPersistentCache;
            private readonly cSectionHandle mSectionHandle;
            private readonly iPersistentSectionCacheItem mSectionCacheItem;

            public cSectionHandleAdder(cPersistentCache pPersistentCache, cSectionHandle pSectionHandle, iPersistentSectionCacheItem pSectionCacheItem)
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
            private readonly iPersistentSectionCacheItem mSectionCacheItem;

            public cSectionIdAdder(cPersistentCache pPersistentCache, cSectionId pSectionId, iPersistentSectionCacheItem pSectionCacheItem)
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
