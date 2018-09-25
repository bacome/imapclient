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

            mHeaderCache = pHeaderCache ?? kDefaultHeaderCache;
            mFlagCache = pFlagCache ?? kDefaultFlagCache;
            mSectionCache = pSectionCache ?? kDefaultSectionCache;
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

        internal void Open(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Open), pMailboxId);

            // (TODO) it'd be better if the locking was at the mailbox level
            //  (concurrent dictionary, value contains count and lock)

            lock (mMailboxToSelectedCountLock)
            {
                int lSelectedCount;

                if (mMailboxToSelectedCount.TryGetValue(pMailboxId, out lSelectedCount)) lSelectedCount++;
                else lSelectedCount = 1;

                if (lSelectedCount == 1)
                {
                    mHeaderCache.Open(pMailboxId, lContext);
                    mFlagCache.Open(pMailboxId, lContext);
                    mSectionCache.Open(pMailboxId, lContext);
                }

                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;
            }
        }

        internal void Close(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Close), pMailboxId);

            lock (mMailboxToSelectedCountLock)
            {
                if (!mMailboxToSelectedCount.TryGetValue(pMailboxId, out var lSelectedCount)) throw new cInternalErrorException(lContext);
                lSelectedCount--;
                mMailboxToSelectedCount[pMailboxId] = lSelectedCount;

                if (lSelectedCount == 0)
                {
                    mHeaderCache.Close(pMailboxId, lContext);
                    mFlagCache.Close(pMailboxId, lContext);
                    mSectionCache.Close(pMailboxId, lContext);
                }
            }
        }

        internal uint GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDValidity), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            uint lResult = 0;
            uint? lTemp;

            if ((lTemp = mHeaderCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp.Value;
            if ((lTemp = mFlagCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp.Value;
            if ((lTemp = mSectionCache.GetUIDValidity(pMailboxId, lContext)) > lResult) lResult = lTemp.Value;

            return lResult;
        }

        internal ulong GetHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetHighestModSeq), pMailboxUID);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));

            ulong? lResult = null;
            ulong? lTemp;

            if ((lTemp = mHeaderCache.GetHighestModSeq(pMailboxUID, lContext)) != null) lResult = lTemp.Value;
            if ((lTemp = mFlagCache.GetHighestModSeq(pMailboxUID, lContext)) != null && (lResult == null || lTemp < lResult)) lResult = lTemp.Value;
            if ((lTemp = mSectionCache.GetHighestModSeq(pMailboxUID, lContext)) != null && (lResult == null || lTemp < lResult)) lResult = lTemp.Value;

            return lResult ?? 0;
        }

        internal HashSet<cUID> GetUIDs(cMailboxUID pMailboxUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(GetUIDs), pMailboxUID);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));

            var lUIDs = mHeaderCache.GetUIDs(pMailboxUID, lContext);
            lUIDs.UnionWith(mFlagCache.GetUIDs(pMailboxUID, lContext));
            lUIDs.UnionWith(mSectionCache.GetUIDs(pMailboxUID, lContext));

            foreach (var lUID in lUIDs) if (lUID == null || lUID.UIDValidity != pMailboxUID.UIDValidity) throw new cUnexpectedPersistentCacheActionException(lContext);

            return lUIDs;
        }

        internal void MessageExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessageExpunged), pMessageHandle);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));

            if (pMessageHandle.UID != null)
            {
                cUID[] lUIDs = new cUID[] { pMessageHandle.UID };

                mHeaderCache.MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);
                mFlagCache.MessagesExpunged(pMessageHandle.MessageCache.MailboxHandle.MailboxId, lUIDs, lContext);
            }

            mSectionCache.MessageExpunged(pMessageHandle, lContext);
        }

        internal bool Vanished(cMailboxId pMailboxId, uint pUIDValidity, cSequenceSet pKnownUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Vanished), pMailboxId, pUIDValidity, pKnownUIDs);

            if (!cUIntList.TryConstruct(pKnownUIDs, -1, true, out var lUInts)) return false;

            var lUIDs = new List<cUID>(from lUID in lUInts select new cUID(pUIDValidity, lUID));

            mHeaderCache.MessagesExpunged(pMailboxId, lUIDs, lContext);
            mFlagCache.MessagesExpunged(pMailboxId, lUIDs, lContext);
            mSectionCache.MessagesExpunged(pMailboxId, lUIDs, lContext);

            return true;
        }

        internal void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(MessagesExpunged), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            mHeaderCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
            mFlagCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
            mSectionCache.MessagesExpunged(pMailboxId, pUIDs, lContext);
        }

        internal void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetUIDValidity), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pUIDValidity));

            mHeaderCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            mFlagCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
            mSectionCache.SetUIDValidity(pMailboxId, pUIDValidity, lContext);
        }

        internal void SetHighestModSeq(cMailboxUID pMailboxUID, ulong pHighestModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(SetHighestModSeq), pMailboxUID, pHighestModSeq);

            if (pMailboxUID == null) throw new ArgumentNullException(nameof(pMailboxUID));
            if (pHighestModSeq < 1) throw new ArgumentOutOfRangeException(nameof(pHighestModSeq));

            mHeaderCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            mFlagCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
            mSectionCache.SetHighestModSeq(pMailboxUID, pHighestModSeq, lContext);
        }

        internal void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);

            if (pSourceMailboxId == null) throw new ArgumentNullException(nameof(pSourceMailboxId));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));

            mHeaderCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            mFlagCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
            mSectionCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, lContext);
        }

        internal void Rename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            mHeaderCache.Rename(pMailboxId, pMailboxName, pParentContext);
            mFlagCache.Rename(pMailboxId, pMailboxName, pParentContext);
            mSectionCache.Rename(pMailboxId, pMailboxName, pParentContext);
        }

        internal void Reconcile(cMailboxId pMailboxId, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));

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