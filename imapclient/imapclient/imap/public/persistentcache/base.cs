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

        private readonly ConcurrentDictionary<cSectionHandle, cSectionCacheItem> mSectionHandleToItem = new ConcurrentDictionary<cSectionHandle, cSectionCacheItem>();

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

        protected internal abstract void MessageExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext);
        protected internal abstract void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext);
        protected internal abstract void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);
        protected internal abstract void SetHighestModSeq(cMailboxUID pMailboxUID, ulong pHighestModSeq, cTrace.cContext pParentContext);
        protected internal abstract void ClearHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext); // for flags, from now until re-opened
        protected internal abstract void ClearCache(cMailboxId pMailboxId, cTrace.cContext pParentContext); // including uidvalidity and highestmodseq

        protected abstract HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext);

        protected internal virtual void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);
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

            var lMailboxNames = YGetMailboxNames(pMailboxId.AccountId, lContext);

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
            // overrides must take account of the fact that duplicates could be created by any rename done
        }

        protected internal abstract iPersistentHeaderCacheItem GetHeaderCacheItem(cMessageUID pMessageUID, cTrace.cContext pParentContext);
        protected internal abstract iPersistentFlagCacheItem GetFlagCacheItem(cMessageUID pMessageUID, cTrace.cContext pParentContext);

        internal bool TryGetSectionReader(cSectionId pSectionId, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            ;?;
        }

        protected abstract bool YTryGetSectionReader(cSectionId pSectionId, out Stream rStream, cTrace.cContext pParentContext);

        internal bool TryGetSectionReader(cSectionHandle pSectionHandle, out cSectionReader rSectionReader, cTrace.cContext pParentContext)
        {
            ;?; // this is completely internal
        }

        internal cSectionReaderWriter GetSectionReaderWriter(cTrace.cContext pParentContext)
        {
            ;?;
        }


        protected abstract cSectionItem YGetNewSectionItem(cTrace.cContext pParentContext);

        protected internal abstract void AddSectionItem(cSectionId pSectionId, cSectionItem pSectionItem);

        internal void AddSectionItem(cSectionHandle pSectionHandle, cSectionItem pSectionItem)
        {
            ;?;

            // after adding it, check that it doesn't have a UID
            //  if it does, do the transfer imediately
        }

        internal void hasuidnow();



        internal iPersistentSectionCacheItem GetSectionReaderWriter(cSectionHandle pSectionHandle, cTrace.cContext pParentContext)
        {



            ;?; // the concrete class has to provide a writable item, but it 
            ;?; // handlehasuid API here moves items into cache and then removes from our list => check ourlist then sc
        }

        protected iPersistentSectionCacheNewItem GetNewSectionCacheItem(cTrace.cContext pParentContext)
        {
            ;?;
        }



        ;?; // handlehasuid API here moves items into cache and then removes from our list => check ourlist then sc


















        internal void Reconcile(cMailboxId pMailboxId, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Reconcile), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));

            ZReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);

            var lMailboxNames = YGetMailboxNames(pMailboxId.AccountId, lContext);

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

            var lMailboxNames = YGetMailboxNames(pAccountId, lContext);

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

        ;?;
        

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
    }

}
