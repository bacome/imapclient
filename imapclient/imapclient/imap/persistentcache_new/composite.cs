using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal abstract class cCompositePersistentCache : cPersistentCache
    {
        private static readonly cHeaderCache kDefaultHeaderCache = new cDefaultHeaderCache();
        private static readonly cFlagCache kDefaultFlagCache = new cDefaultFlagCache();
        private static readonly cSectionCache kDefaultSectionCache = new cDefaultSectionCache();

        private readonly cHeaderCache mHeaderCache;
        private readonly cFlagCache mFlagCache;
        private readonly cSectionCache mSectionCache;

        public cCompositePersistentCache(cHeaderCache pHeaderCache, cFlagCache pFlagCache, cSectionCache pSectionCache)
        {
            mHeaderCache = pHeaderCache ?? kDefaultHeaderCache;
            mFlagCache = pFlagCache ?? kDefaultFlagCache;
            mSectionCache = pSectionCache ?? kDefaultSectionCache;
        }

        protected sealed override void YOpen(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(YOpen), pMailboxId);

            mHeaderCache.Open(pMailboxId, lContext);
            mFlagCache.Open(pMailboxId, lContext);
            mSectionCache.Open(pMailboxId, lContext);
        }

        protected sealed override void YClose(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(YClose), pMailboxId);

            mHeaderCache.Close(pMailboxId, lContext);
            mFlagCache.Close(pMailboxId, lContext);
            mSectionCache.Close(pMailboxId, lContext);
        }

        public sealed override uint GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(GetUIDValidity), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            uint lResult = 0;
            uint? lTemp;

            if ((lTemp = mHeaderCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp.Value;
            if ((lTemp = mFlagCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp.Value;
            if ((lTemp = mSectionCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp.Value;

            return lResult;
        }

        public sealed override ulong GetHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(GetHighestModSeq), pMailboxUID);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));

            ulong? lResult = null;
            ulong? lTemp;

            if ((lTemp = mHeaderCache.GetHighestModSeq(pMailboxUID, lContext)) != null) lResult = lTemp.Value;
            if ((lTemp = mFlagCache.GetHighestModSeq(pMailboxUID, lContext)) != null && (lResult == null || lTemp < lResult)) lResult = lTemp.Value;
            if ((lTemp = mSectionCache.GetHighestModSeq(pMailboxUID, lContext)) != null && (lResult == null || lTemp < lResult)) lResult = lTemp.Value;

            return lResult ?? 0;
        }

        protected sealed override HashSet<cUID> YGetUIDs(cMailboxUID pMailboxUID, bool pForCachedFlagsOnly, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(YGetUIDs), pMailboxUID, pForCachedFlagsOnly);

            var lUIDs = mFlagCache.GetUIDs(pMailboxUID, lContext);
            if (pForCachedFlagsOnly) return lUIDs;

            lUIDs.UnionWith(mHeaderCache.GetUIDs(pMailboxUID, lContext));
            lUIDs.UnionWith(mSectionCache.GetUIDs(pMailboxUID, lContext));

            return lUIDs;
        }

        protected internal sealed override void MessageExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
        }

        protected internal sealed override void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(MessagesExpunged), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            mHeaderCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
            mFlagCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
            mSectionCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
        }

        protected internal sealed override void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(SetUIDValidity), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));

            mHeaderCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            mFlagCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            mSectionCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
        }

        protected internal sealed override void SetHighestModSeq(cMailboxUID pMailboxUID, ulong pHighestModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(SetHighestModSeq), pMailboxUID, pHighestModSeq);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));
            if (pHighestModSeq < 1) throw new ArgumentOutOfRangeException(nameof(pHighestModSeq));

            mHeaderCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            mFlagCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            mSectionCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
        }

        protected internal sealed override void ClearHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext) => mFlagCache.ClearHighestModSeq(pMailboxUID, pParentContext);

        protected internal sealed override void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);

            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            mHeaderCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            mFlagCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            mSectionCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
        }

        protected internal sealed override void Rename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(Rename), pMailboxId, pMailboxName);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            mHeaderCache.Rename(pMailboxId, pMailboxName, lContext);
            mFlagCache.Rename(pMailboxId, pMailboxName, lContext);
            mSectionCache.Rename(pMailboxId, pMailboxName, lContext);
        }

        protected override void YReconcile(cMailboxId pMailboxId, HashSet<cMailboxName> pAllExistentChildMailboxNames, HashSet<cMailboxName> pAllSelectableChildMailboxNames, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCompositePersistentCache), nameof(YReconcile), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllExistentChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllExistentChildMailboxNames));
            if (pAllSelectableChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllSelectableChildMailboxNames));

            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            mHeaderCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mFlagCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mSectionCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
        }







        internal void Reconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pNotPrefixedWith == null) throw new ArgumentNullException(nameof(pNotPrefixedWith));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));

            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            mHeaderCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mFlagCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mSectionCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
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

        ;?; // check not called when uidnotsticky
        internal void MessageHandleUIDSet(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            // this is to let the section cache know what the UID is for the handle - we may have been filing things under the handle so this gives a chance for those things to be moved to be filed under the UID
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageHandleUIDSet), pMessageHandle);
            mSectionCache.MessageHandleUIDSet(pMessageHandle, lContext);
        }

        internal void MessageCacheDeactivated(iMessageCache pMessageCache, cTrace.cContext pParentContext)
        {
            // this is to let the section cache know that any data stored against handles in the cache can be trashed
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageCacheDeactivated), pMessageCache);
            mSectionCache.MessageCacheDeactivated(pMessageCache, lContext);
        }

        ;?; // check not called when uidnotsticky
        internal cHeaderCacheItem GetHeaderCacheItem(cMessageUID pMessageUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetHeaderCacheItem), pMessageUID);
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            return mHeaderCache.GetItem(pMessageUID, lContext) ?? throw new cUnexpectedPersistentCacheActionException(lContext);
        }

        ;?; // check not called when uidnotsticky
        internal cFlagCacheItem GetFlagCacheItem(cMessageUID pMessageUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetFlagCacheItem), pMessageUID);
            if (pMessageUID == null) throw new ArgumentNullException(nameof(pMessageUID));
            return mFlagCache.GetItem(pMessageUID, lContext) ?? throw new cUnexpectedPersistentCacheActionException(lContext);
        }

        internal bool TryGetSectionLength(cSectionId pSectionId, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionLength), pSectionId);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            return mSectionCache.TryGetSectionLength(pSectionId, out rLength, lContext);
        }

        internal bool TryGetSectionReader(cSectionId pSectionId, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionReader), pSectionId);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            return mSectionCache.TryGetSectionReader(pSectionId, out rReader, lContext);
        }

        internal cSectionCacheItem GetNewSectionCacheItem(cSectionId pSectionId, bool pUIDNotSticky, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetNewSectionCacheItem), pSectionId, pUIDNotSticky);
            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));
            return mSectionCache.GetNewItem(pSectionId, pUIDNotSticky, lContext) ?? throw new cUnexpectedPersistentCacheActionException(lContext); 
        }

        internal bool TryGetSectionLength(cSectionHandle pSectionHandle, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetSectionLength), pSectionHandle);
            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);
            return mSectionCache.TryGetSectionLength(pSectionHandle, out rLength, lContext);
        }

        internal bool TryGetSectionReader(cSectionHandle pSectionHandle, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetSectionReader), pSectionHandle);
            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);
            return mSectionCache.TryGetSectionReader(pSectionHandle, out rReader, lContext);
        }

        internal cSectionCacheItem GetNewSectionCacheItem(cSectionHandle pSectionHandle, bool pUIDNotSticky, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewSectionCacheItem), pSectionHandle);
            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);
            return mSectionCache.GetNewItem(pSectionHandle, pUIDNotSticky, lContext) ?? throw new cUnexpectedPersistentCacheActionException(lContext);
        }

        internal void TryAddSectionCacheItem(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryAddSectionCacheItem), pItem);
            if (pItem == null) throw new ArgumentNullException(nameof(pItem));
            mSectionCache.TryAddItem(pItem, lContext);
        }

        public override string ToString() => $"{nameof(cPersistentCache)}({mHeaderCache},{mSectionCache},{mFlagCache})";
    }
}