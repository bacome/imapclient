using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cPersistentCache
    {
        private static readonly cHeaderCache kDefaultHeaderCache = new cDefaultHeaderCache();
        private static readonly cFlagCache kDefaultFlagCache = new cDefaultFlagCache();
        private static readonly cSectionCache kDefaultSectionCache = new cDefaultSectionCache();

        public readonly string InstanceName;
        private readonly cTrace.cContext mRootContext;

        private readonly cHeaderCache mHeaderCache;
        private readonly cFlagCache mFlagCache;
        private readonly cSectionCache mSectionCache;

        private readonly object mMailboxToSelectedCountLock = new object();
        private readonly Dictionary<cMailboxId, int> mMailboxToSelectedCount = new Dictionary<cMailboxId, int>();

        public cPersistentCache(string pInstanceName, cHeaderCache pHeaderCache, cFlagCache pFlagCache, cSectionCache pSectionCache)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);

            mHeaderCache = pHeaderCache;
            mFlagCache = pFlagCache;
            mSectionCache = pSectionCache;
        }

        /* these to comment back in later (commented out to stop me using the wrong ones)
        public uint GetUIDValidity(cMailboxId pMailboxId) => GetUIDValidity(pMailboxId, mRootContext);
        public ulong GetHighestModSeq(cMailboxUID pMailboxUID) => GetHighestModSeq(pMailboxUID, mRootContext);
        public HashSet<cUID> GetUIDs(cMailboxUID pMailboxUID) => GetUIDs(pMailboxUID, mRootContext);
        public bool TryGetHeaderCacheItem(cMessageUID pMessageUID, out cHeaderCacheItem rHeaderCacheItem) => TryGetHeaderCacheItem(pMessageUID, out rHeaderCacheItem, mRootContext);
        public bool TryGetModSeqFlags(cMessageUID pMessageUID, out cModSeqFlags rModSeqFlags) => TryGetModSeqFlags(pMessageUID, out rModSeqFlags, mRootContext);
        public bool TryGetSectionLength(cSectionId pSectionId, out long rLength) => TryGetSectionLength(pSectionId, out rLength, mRootContext);

        public bool TryGetSectionReader(cSectionId pSectionId, out Stream rStream)
        {
            if (TryGetSectionReader(pSectionId, out var lReader, mRootContext))
            {
                rStream = lReader;
                return true;
            }

            rStream = null;
            return false;
        } */

        internal void RecordMailboxSelected(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(RecordMailboxSelected), pMailboxId);

            lock (mMailboxToSelectedCountLock)
            {
                int lSelectedCount;

                if (mMailboxToSelectedCount.TryGetValue(pMailboxId, out lSelectedCount)) lSelectedCount++;
                else lSelectedCount = 1;

                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;
            }
        }

        internal void RecordMailboxUnselected(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(RecordMailboxUnselected), pMailboxId);

            lock (mMailboxToSelectedCountLock)
            {
                if (!mMailboxToSelectedCount.TryGetValue(pMailboxId, out var lSelectedCount)) throw new cInternalErrorException(lContext);
                lSelectedCount--;
                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;

                if (lSelectedCount == 0)
                {
                    ;?; // tell the flag and header caches
                }
            }
        }

        internal uint GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDValidity), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            uint lResult = 0;
            uint lTemp;

            if ((lTemp = kDefaultHeaderCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp;
            if ((lTemp = kDefaultFlagCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp;
            if ((lTemp = kDefaultSectionCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp;

            if (mHeaderCache != null && (lTemp = mHeaderCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp; 
            if (mFlagCache != null && (lTemp = mFlagCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp;
            if (mSectionCache != null && (lTemp = mSectionCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp; 

            return lResult;
        }

        internal ulong GetHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetHighestModSeq), pMailboxUID);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));

            ulong lResult = 0;

            ZGetHighestModSeq(kDefaultHeaderCache, pMailboxUID, ref lResult, lContext);
            ZGetHighestModSeq(kDefaultFlagCache, pMailboxUID, ref lResult, lContext);
            ZGetHighestModSeq(kDefaultSectionCache, pMailboxUID, ref lResult, lContext);

            if (mHeaderCache != null) ZGetHighestModSeq(mHeaderCache, pMailboxUID, ref lResult, lContext);
            if (mFlagCache != null) ZGetHighestModSeq(mFlagCache, pMailboxUID, ref lResult, lContext);
            if (mSectionCache != null) ZGetHighestModSeq(mSectionCache, pMailboxUID, ref lResult, lContext);

            return lResult;
        }

        private void ZGetHighestModSeq(cPersistentCacheComponent pComponent, cMailboxUID pMailboxUID, ref ulong rHighestModSeq, cTrace.cContext pParentContext)
        {
            var lHighestModSeq = pComponent.GetHighestModSeq(pMailboxUID, pParentContext);

            if (lHighestModSeq == 0 && pComponent.GetUIDs(pMailboxUID, pParentContext).Count == 0) return;

            if (rHighestModSeq == 0)
            {
                rHighestModSeq = lHighestModSeq;
                return;
            }

            if (lHighestModSeq < rHighestModSeq) rHighestModSeq = lHighestModSeq;
        }

        internal HashSet<cUID> GetUIDs(cMailboxUID pMailboxUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDs), pMailboxUID);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));

            var lUIDs = kDefaultHeaderCache.GetUIDs(pMailboxUID, lContext);
            lUIDs.UnionWith(kDefaultFlagCache.GetUIDs(pMailboxUID, lContext));
            lUIDs.UnionWith(kDefaultSectionCache.GetUIDs(pMailboxUID, lContext));

            if (mHeaderCache != null) lUIDs.UnionWith(mHeaderCache.GetUIDs(pMailboxUID, lContext));
            if (mFlagCache != null) lUIDs.UnionWith(mFlagCache.GetUIDs(pMailboxUID, lContext));
            if (mSectionCache != null)  lUIDs.UnionWith(mSectionCache.GetUIDs(pMailboxUID, lContext));

            return lUIDs;
        }

        internal void MessageExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageExpunged), pMessageHandle);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));

            if (pMessageHandle.UID != null)
            {
                cUID[] lUIDs = new cUID[] { pMessageHandle.UID };

                kDefaultHeaderCache.MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);
                kDefaultFlagCache.MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);

                mHeaderCache?.MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);
                mFlagCache?.MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);
            }

            kDefaultSectionCache.MessageExpunged(pMessageHandle, lContext);
            mSectionCache?.MessageExpunged(pMessageHandle, lContext);
        }

        internal void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessagesExpunged), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            kDefaultHeaderCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
            kDefaultFlagCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
            kDefaultSectionCache.MessagesExpunged(pMailboxId, pUIDs, lContext);

            mHeaderCache?.MessagesExpunged(pMailboxId, pUIDs, lContext);
            mFlagCache?.MessagesExpunged(pMailboxId, pUIDs, lContext);
            mSectionCache?.MessagesExpunged(pMailboxId, pUIDs, lContext);
        }

        internal void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetUIDValidity), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity < 1) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));

            kDefaultHeaderCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            kDefaultFlagCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            kDefaultSectionCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);

            mHeaderCache?.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            mFlagCache?.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            mSectionCache?.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
        }

        internal void SetHighestModSeq(cMailboxUID pMailboxUID, ulong pHighestModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetHighestModSeq), pMailboxUID, pHighestModSeq);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));
            if (pHighestModSeq < 1) throw new ArgumentOutOfRangeException(nameof(pHighestModSeq));

            kDefaultHeaderCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            kDefaultFlagCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            kDefaultSectionCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);

            mHeaderCache?.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            mFlagCache?.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            mSectionCache?.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
        }

        internal void ClearCachedItems(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ClearCachedItems), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            kDefaultHeaderCache.ClearCachedItems(pMailboxId, lContext);
            kDefaultFlagCache.ClearCachedItems(pMailboxId, lContext);
            kDefaultSectionCache.ClearCachedItems(pMailboxId, lContext);

            mHeaderCache?.ClearCachedItems(pMailboxId, lContext);
            mFlagCache?.ClearCachedItems(pMailboxId, lContext);
            mSectionCache?.ClearCachedItems(pMailboxId, lContext); 
        }

        internal void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);

            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            kDefaultHeaderCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            kDefaultFlagCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            kDefaultSectionCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);

            mHeaderCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            mFlagCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            mSectionCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
        }

        internal void Rename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            kDefaultHeaderCache.Rename(pMailboxId, pMailboxName, pParentContext);
            kDefaultFlagCache.Rename(pMailboxId, pMailboxName, pParentContext);
            kDefaultSectionCache.Rename(pMailboxId, pMailboxName, pParentContext);

            mHeaderCache?.Rename(pMailboxId, pMailboxName, pParentContext);
            mFlagCache?.Rename(pMailboxId, pMailboxName, pParentContext);
            mSectionCache?.Rename(pMailboxId, pMailboxName, pParentContext);
        }

        internal void Reconcile(cMailboxId pMailboxId, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));

            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            kDefaultHeaderCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            kDefaultFlagCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            kDefaultSectionCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mHeaderCache?.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mFlagCache?.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mSectionCache?.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
        }

        internal void Reconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pNotPrefixedWith == null) throw new ArgumentNullException(nameof(pNotPrefixedWith));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));

            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            kDefaultHeaderCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            kDefaultFlagCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            kDefaultSectionCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mHeaderCache?.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mFlagCache?.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mSectionCache?.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
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

        internal void MessageHandleUIDSet(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            // this is to let the section cache know what the UID is for the handle - we may have been filing things under the handle so this gives a chance for those things to be moved to be filed under the UID
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageHandleUIDSet), pMessageHandle);

            ;?; // only use the cache in action

            kDefaultSectionCache.MessageHandleUIDSet(pMessageHandle, lContext);
            mSectionCache?.MessageHandleUIDSet(pMessageHandle, lContext);
        }

        internal void MessageCacheDeactivated(iMessageCache pMessageCache, cTrace.cContext pParentContext)
        {
            // this is to let the section cache know that any data stored against handles in the cache can be trashed
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageCacheDeactivated), pMessageCache);

            ;?; // only use the cache in action
            kDefaultSectionCache.MessageCacheDeactivated(pMessageCache, lContext);
            mSectionCache?.MessageCacheDeactivated(pMessageCache, lContext);
        }

        internal cHeaderCacheItem GetHeaderCacheItem(cMessageUID pMessageUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetHeaderCacheItem), pMessageUID);
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            if (mHeaderCache == null) return kDefaultHeaderCache.TryGetHeaderCacheItem(pMessageUID, out rHeaderCacheItem, lContext);
            return mHeaderCache.TryGetHeaderCacheItem(pMessageUID, out rHeaderCacheItem, lContext);
        }

        internal bool TryGetModSeqFlags(cMessageUID pMessageUID, out cModSeqFlags rModSeqFlags, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetModSeqFlags), pMessageUID);
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            if (mFlagCache == null) return kDefaultFlagCache.TryGetModSeqFlags(pMessageUID, out rModSeqFlags, lContext);
            return mFlagCache.TryGetModSeqFlags(pMessageUID, out rModSeqFlags, lContext);
        }

        internal void SetModSeqFlags(cMessageUID pMessageUID, cModSeqFlags pModSeqFlags, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetModSeqFlags), pMessageUID, pModSeqFlags);
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            if (pModSeqFlags == null) throw new ArgumentNullException(nameof(pModSeqFlags));
            if (mFlagCache == null) kDefaultFlagCache.SetModSeqFlags(pMessageUID, pModSeqFlags, lContext);
            else mFlagCache.SetModSeqFlags(pMessageUID, pModSeqFlags, lContext);
        }

        internal bool TryGetSectionLength(cSectionId pSectionId, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionLength), pSectionId);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            if (mSectionCache == null) return kDefaultSectionCache.TryGetSectionLength(pSectionId, out rLength, lContext);
            return mSectionCache.TryGetSectionLength(pSectionId, out rLength, lContext);
        }

        internal bool TryGetSectionReader(cSectionId pSectionId, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionReader), pSectionId);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            if (mSectionCache == null) return kDefaultSectionCache.TryGetSectionReader(pSectionId, out rReader, lContext);
            return mSectionCache.TryGetSectionReader(pSectionId, out rReader, lContext);
        }

        internal cSectionCacheItem GetNewSectionCacheItem(cSectionId pSectionId, bool pUIDNotSticky, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetNewSectionCacheItem), pSectionId, pUIDNotSticky);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            if (mSectionCache == null) return kDefaultSectionCache.GetNewItem(pSectionId, pUIDNotSticky, lContext);
            return mSectionCache.GetNewItem(pSectionId, pUIDNotSticky, lContext); 
        }

        internal bool TryGetSectionLength(cSectionHandle pSectionHandle, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetSectionLength), pSectionHandle);
            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);
            if (mSectionCache == null) return kDefaultSectionCache.TryGetSectionLength(pSectionHandle, out rLength, lContext);
            return mSectionCache.TryGetSectionLength(pSectionHandle, out rLength, lContext);
        }

        internal bool TryGetSectionReader(cSectionHandle pSectionHandle, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetSectionReader), pSectionHandle);
            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);
            if (mSectionCache == null) return kDefaultSectionCache.TryGetSectionReader(pSectionHandle, out rReader, lContext);
            return mSectionCache.TryGetSectionReader(pSectionHandle, out rReader, lContext);
        }

        internal cSectionCacheItem GetNewSectionCacheItem(cSectionHandle pSectionHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewSectionCacheItem), pSectionHandle);
            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);
            if (mSectionCache == null) return kDefaultSectionCache.GetNewItem(pSectionHandle, lContext);
            return mSectionCache.GetNewItem(pSectionHandle, lContext);
        }

        internal void TryAddSectionCacheItem(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryAddSectionCacheItem), pItem);
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            if (mSectionCache == null) kDefaultSectionCache.TryAddItem(pItem, lContext);
            else mSectionCache.TryAddItem(pItem, lContext);
        }

        public override string ToString() => $"{nameof(cPersistentCache)}({mHeaderCache},{mSectionCache},{mFlagCache})";
    }
}