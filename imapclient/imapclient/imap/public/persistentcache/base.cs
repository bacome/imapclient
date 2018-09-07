using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cPersistentCache
    {
        private static readonly cSectionCache kDefaultSectionCache = new cDefaultSectionCache();

        private readonly cHeaderCache mHeaderCache; 
        private readonly cSectionCache mSectionCache;
        private readonly cFlagCache mFlagCache; 

        internal cPersistentCache()
        {
            mHeaderCache = null;
            mSectionCache = null;
            mFlagCache = null;
        }

        public cPersistentCache(cSectionCache pSectionCache)
        {
            mHeaderCache = null;
            mSectionCache = pSectionCache ?? throw new ArgumentNullException(nameof(pSectionCache));
            mFlagCache = null;
        }

        internal void MessageExpunged(cMailboxId pMailboxId, cUID pUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageExpunged), pMailboxId, pUID);

            try { kDefaultSectionCache.MessageExpunged(pMailboxId, pUID, pParentContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.MessageExpunged(pMailboxId, pUID, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.MessageExpunged(pMailboxId, pUID, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.MessageExpunged(pMailboxId, pUID, pParentContext); }
            catch (Exception e) { lContext.TraceException("flag cache threw", e); }
        }

        internal void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessagesExpunged), pMailboxId);

            try { kDefaultSectionCache.MessagesExpunged(pMailboxId, pUIDs, pParentContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.MessagesExpunged(pMailboxId, pUIDs, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.MessagesExpunged(pMailboxId, pUIDs, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.MessagesExpunged(pMailboxId, pUIDs, pParentContext); }
            catch (Exception e) { lContext.TraceException("flag cache threw", e); }
        }

        internal void SetMailboxUIDValidity(cMailboxId pMailboxId, long pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetMailboxUIDValidity), pMailboxId, pUIDValidity);

            try { kDefaultSectionCache.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext); }
            catch (Exception e) { lContext.TraceException("flag cache threw", e); }
        }

        internal void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);

            try { kDefaultSectionCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, pParentContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }
        }

        internal void Rename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            kDefaultSectionCache.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
            mHeaderCache?.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
            mSectionCache?.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
            mFlagCache?.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
        }

        internal void Reconcile(cMailboxId pMailboxId, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));
            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            kDefaultSectionCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mHeaderCache?.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mSectionCache?.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mFlagCache?.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
        }

        internal void Reconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pNotPrefixedWith == null) throw new ArgumentNullException(nameof(pNotPrefixedWith));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));
            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            kDefaultSectionCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mHeaderCache?.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mSectionCache?.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            mFlagCache?.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
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

        internal HashSet<cUID> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDs), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            var lUIDs = new HashSet<cUID>();

            try { lUIDs.UnionWith(kDefaultSectionCache.GetUIDs(pMailboxId, pUIDValidity, lContext)); }
            catch (Exception e) { lContext.TraceException(nameof(kDefaultSectionCache), e); }

            if (mHeaderCache != null)
            {
                try { lUIDs.UnionWith(mHeaderCache.GetUIDs(pMailboxId, pUIDValidity, lContext)); }
                catch (Exception e) { lContext.TraceException(nameof(mHeaderCache), e); }
            }

            if (mSectionCache != null)
            {
                try { lUIDs.UnionWith(mSectionCache.GetUIDs(pMailboxId, pUIDValidity, lContext)); }
                catch (Exception e) { lContext.TraceException(nameof(mSectionCache), e); }
            }

            if (mFlagCache != null)
            {
                try { lUIDs.UnionWith(mFlagCache.GetUIDs(pMailboxId, pUIDValidity, lContext)); }
                catch (Exception e) { lContext.TraceException(nameof(mFlagCache), e); }
            }

            return lUIDs;
        }

        internal bool TryGetSectionCacheItemLength(cSectionId pSectionId, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionCacheItemLength), pSectionId);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            if (mSectionCache != null && mSectionCache.TryGetItemLength(pSectionId, out rLength, lContext)) return true;
            if (kDefaultSectionCache.TryGetItemLength(pSectionId, out rLength, lContext)) return true;

            rLength = -1;
            return false;
        }

        internal bool TryGetSectionCacheItemReader(cSectionId pSectionId, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(TryGetSectionCacheItemReader), pSectionId);

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

            if (mSectionCache != null && mSectionCache.TryGetItemReader(pSectionId, out rReader, lContext)) return true;
            if (kDefaultSectionCache.TryGetItemReader(pSectionId, out rReader, lContext)) return true;

            rReader = null;
            return false;
        }

        internal cSectionCacheItem GetNewSectionCacheItem(cSectionId pSectionId, bool pUIDNotSticky, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetNewSectionCacheItem), pSectionId, pUIDNotSticky);

            if (mSectionCache != null)
            {
                try { return mSectionCache.GetNewItem(pSectionId, pUIDNotSticky, lContext); }
                catch (Exception e) { lContext.TraceException("section cache threw", e); }
            }

            return kDefaultSectionCache.GetNewItem(pSectionId, pUIDNotSticky, lContext);
        }

        internal bool TryGetSectionCacheItemLength(cSectionHandle pSectionHandle, out long rLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetSectionCacheItemLength), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            if (mSectionCache != null && mSectionCache.TryGetItemLength(pSectionHandle, out rLength, lContext)) return true;
            if (kDefaultSectionCache.TryGetItemLength(pSectionHandle, out rLength, lContext)) return true;

            rLength = -1;
            return false;
        }

        internal bool TryGetSectionCacheItemReader(cSectionHandle pSectionHandle, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryGetSectionCacheItemReader), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            if (mSectionCache != null && mSectionCache.TryGetItemReader(pSectionHandle, out rReader, lContext)) return true;
            if (kDefaultSectionCache.TryGetItemReader(pSectionHandle, out rReader, lContext)) return true;

            rReader = null;
            return false;
        }

        internal cSectionCacheItem GetNewSectionCacheItem(cSectionHandle pSectionHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(GetNewSectionCacheItem), pSectionHandle);

            if (pSectionHandle == null) throw new ArgumentNullException(nameof(pSectionHandle));
            if (pSectionHandle.MessageHandle.Expunged) throw new cMessageExpungedException(pSectionHandle.MessageHandle);

            if (mSectionCache != null)
            {
                try { return mSectionCache.GetNewItem(pSectionHandle, lContext); }
                catch (Exception e) { lContext.TraceException("section cache threw", e); }
            }

            return kDefaultSectionCache.GetNewItem(pSectionHandle, lContext);
        }

        internal void TryAddSectionCacheItem(cSectionCacheItem pItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(TryAddSectionCacheItem), pItem);

            if (pItem == null) throw new ArgumentNullException(nameof(pItem));

            if (ReferenceEquals(pItem.Cache, mSectionCache)) mSectionCache.TryAddItem(pItem, lContext);
            else if (ReferenceEquals(pItem.Cache, kDefaultSectionCache)) kDefaultSectionCache.TryAddItem(pItem, lContext);
            else throw new ArgumentOutOfRangeException(nameof(pItem));
        }

        public override string ToString() => $"{nameof(cPersistentCache)}({mHeaderCache},{mSectionCache},{mFlagCache})";
    }
}