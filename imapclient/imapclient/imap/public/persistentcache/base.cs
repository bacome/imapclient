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

        public cPersistentCache(cHeaderCache pHeaderCache, cSectionCache pSectionCache, cFlagCache pFlagCache)
        {
            mHeaderCache = pHeaderCache;
            mSectionCache = pSectionCache;
            mFlagCache = pFlagCache;
        }

        // TODO: add a constructor for a directory cache that sets each item to the directory

        internal uint GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDValidity), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            uint lResult = 0;
            uint lTemp;

            try { if ((lTemp = kDefaultSectionCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp; }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            if (mHeaderCache != null)
            {
                try { if ((lTemp = mHeaderCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp; }
                catch (Exception e) { lContext.TraceException("header cache threw", e); }
            }

            if (mSectionCache != null)
            {
                try { if ((lTemp = mSectionCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp; }
                catch (Exception e) { lContext.TraceException("section cache threw", e); }
            }

            if (mFlagCache != null)
            {
                try { if ((lTemp = mFlagCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp; }
                catch (Exception e) { lContext.TraceException("flag cache threw", e); }
            }

            return lResult;
        }

        internal ulong GetHighestModSeq(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetHighestModSeq), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity < 1) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));

            ulong lResult = 0;

            try { ZGetHighestModSeq(kDefaultSectionCache, pMailboxId, pUIDValidity, ref lResult, lContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            if (mHeaderCache != null)
            {
                try { ZGetHighestModSeq(mHeaderCache, pMailboxId, pUIDValidity, ref lResult, lContext); }
                catch (Exception e) { lContext.TraceException("header cache threw", e); }
            }

            if (mSectionCache != null)
            {
                try { ZGetHighestModSeq(mSectionCache, pMailboxId, pUIDValidity, ref lResult, lContext); }
                catch (Exception e) { lContext.TraceException("section cache threw", e); }
            }

            if (mFlagCache != null)
            {
                try { ZGetHighestModSeq(mFlagCache, pMailboxId, pUIDValidity, ref lResult, lContext); }
                catch (Exception e) { lContext.TraceException("flag cache threw", e); }
            }

            return lResult;
        }

        private void ZGetHighestModSeq(cPersistentCacheComponent pComponent, cMailboxId pMailboxId, uint pUIDValidity, ref ulong pHighestModSeq, cTrace.cContext pParentContext)
        {
            var lHighestModSeq = pComponent.GetHighestModSeq(pMailboxId, pUIDValidity, pParentContext);

            if (lHighestModSeq == 0) return;

            if (pHighestModSeq == 0)
            {
                pHighestModSeq = lHighestModSeq;
                return;
            }

            if (lHighestModSeq < pHighestModSeq) pHighestModSeq = lHighestModSeq;
        }

        internal HashSet<cUID> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDs), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity < 1) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));

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

        internal void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessagesExpunged), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            try { kDefaultSectionCache.MessagesExpunged(pMailboxId, pUIDs, lContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.MessagesExpunged(pMailboxId, pUIDs, lContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.MessagesExpunged(pMailboxId, pUIDs, lContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.MessagesExpunged(pMailboxId, pUIDs, lContext); }
            catch (Exception e) { lContext.TraceException("flag cache threw", e); }
        }

        internal void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetUIDValidity), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity < 1) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));

            try { kDefaultSectionCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.SetUIDValidity(pMailboxId, pUIDValidity, lContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.SetUIDValidity(pMailboxId, pUIDValidity, lContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.SetUIDValidity(pMailboxId, pUIDValidity, lContext); }
            catch (Exception e) { lContext.TraceException("flag cache threw", e); }
        }

        internal void SetHighestModSeq(cMailboxId pMailboxId, ulong pHighestModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetHighestModSeq), pMailboxId, pHighestModSeq);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pHighestModSeq < 1) throw new ArgumentOutOfRangeException(nameof(pHighestModSeq));

            try { kDefaultSectionCache.SetHighestModSeq(pMailboxId, pHighestModSeq, lContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.SetHighestModSeq(pMailboxId, pHighestModSeq, lContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.SetHighestModSeq(pMailboxId, pHighestModSeq, lContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.SetHighestModSeq(pMailboxId, pHighestModSeq, lContext); }
            catch (Exception e) { lContext.TraceException("flag cache threw", e); }
        }

        internal void ClearCachedItems(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ClearCachedItems), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            try { kDefaultSectionCache.ClearCachedItems(pMailboxId, lContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.ClearCachedItems(pMailboxId, lContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.ClearCachedItems(pMailboxId, lContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.ClearCachedItems(pMailboxId, lContext); }
            catch (Exception e) { lContext.TraceException("flag cache threw", e); }
        }

        internal void MessageExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageExpunged), pMessageHandle);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));

            try { kDefaultSectionCache.MessageExpunged(pMessageHandle, lContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mSectionCache?.MessageExpunged(pMessageHandle, lContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            if (pMessageHandle.UID != null)
            {
                try { mHeaderCache?.MessageExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, pMessageHandle.UID, lContext); }
                catch (Exception e) { lContext.TraceException("header cache threw", e); }

                try { mFlagCache?.MessageExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, pMessageHandle.UID, lContext); }
                catch (Exception e) { lContext.TraceException("flag cache threw", e); }
            }
        }

        internal void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);

            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));        

            try { kDefaultSectionCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext); }
            catch (Exception e) { lContext.TraceException("default section cache threw", e); }

            try { mHeaderCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { mSectionCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }

            try { mFlagCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }
        }

        internal void Rename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity < 1) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

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

        internal void MessageHandleUIDSet(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            kDefaultSectionCache.MessageHandleUIDSet(pMessageHandle, pParentContext);
            mSectionCache?.MessageHandleUIDSet(pMessageHandle, pParentContext);
        }

        internal void MessageCacheDeactivated(iMessageCache pMessageCache, cTrace.cContext pParentContext)
        {
            kDefaultSectionCache.MessageCacheDeactivated(pMessageCache, pParentContext);
            mSectionCache?.MessageCacheDeactivated(pMessageCache, pParentContext);
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

            if (pSectionId == null) throw new ArgumentNullException(nameof(pSectionId));

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