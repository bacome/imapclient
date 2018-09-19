using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cPersistentCacheComponent
    {
        ;?;
        // implement a component to implement these
        public abstract uint GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext);
        public abstract ulong GetHighestModSeq(cMailboxUID pMailboxUID, cTrace.cContext pParentContext);
        public abstract HashSet<cUID> GetUIDs(cMailboxUID pMailboxUID, cTrace.cContext pParentContext);

        protected internal abstract void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext);

        protected internal abstract void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);
        protected internal abstract void SetHighestModSeq(cMailboxUID pMailboxUID, ulong pHighestModSeq, cTrace.cContext pParentContext);
        protected internal abstract void ClearCachedItems(cMailboxId pMailboxId, cTrace.cContext pParentContext); // including the UIDValidity and HighestModSeq





        /* these are just for the section cache
        protected internal abstract void MessageExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext);
        protected internal abstract void UIDSet(iMessageHandle pMessageHandle, cTrace.cContext pParentContext);
        protected internal abstract void MessageCacheChange
        */

        /*
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(DiscoveredUID), pMessageHandle);
            // in the section cache this may cause the persisting of items (or the deleting of them if they are duplicates)
            // in the header and flag cache this may cause the update of the data in the handle from the cache
            //  (the handle will get API extensions for this)
            // note that this is only called if the mailbox supports persistent UIDs. 
            //  Additionally the flag update API must defend against condstore being off (highestmodseq not set or set to zero) and updates that wind back the modseq.
        } */

        protected internal virtual void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);
        }

        protected abstract HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext);

        protected virtual void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(YRename), pMailboxId, pMailboxName);
            // overrides must take account of the fact that duplicates could be created by any rename done
        }

        internal void Rename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(Rename), pMailboxId, pMailboxName);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            if (pMailboxId.MailboxName.IsInbox)
            {
                ClearCachedItems(pMailboxId, lContext);
                return;
            }

            YRename(pMailboxId, pMailboxName, lContext);
            ClearCachedItems(pMailboxId, lContext);

            var lMailboxNames = YGetMailboxNames(pMailboxId.AccountId, lContext);

            int lStartIndex = pMailboxId.MailboxName.GetDescendantPathPrefix().Length;

            foreach (var lMailboxName in lMailboxNames)
            {
                if (!lMailboxName.IsDescendantOf(pMailboxId.MailboxName)) continue;

                var lNewPath = pMailboxName.GetDescendantPathPrefix() + lMailboxName.Path.Substring(lStartIndex);
                var lNewMailboxName = new cMailboxName(lNewPath, pMailboxName.Delimiter);
                var lOldMailboxId = new cMailboxId(pMailboxId.AccountId, lMailboxName);

                YRename(lOldMailboxId, lNewMailboxName, lContext);
                ClearCachedItems(lOldMailboxId, lContext);
            }
        }

        internal void Reconcile(cMailboxId pMailboxId, HashSet<cMailboxName> pAllExistentChildMailboxNames, HashSet<cMailboxName> pAllSelectableChildMailboxNames, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(Reconcile), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllExistentChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllExistentChildMailboxNames));
            if (pAllSelectableChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllSelectableChildMailboxNames));

            var lMailboxNames = YGetMailboxNames(pMailboxId.AccountId, lContext);

            foreach (var lMailboxName in lMailboxNames)
            {
                if (lMailboxName.IsChildOf(pMailboxId.MailboxName))
                {
                    if (!pAllSelectableChildMailboxNames.Contains(lMailboxName)) ClearCachedItems(new cMailboxId(pMailboxId.AccountId, lMailboxName), lContext); 
                }
                else if (lMailboxName.IsDescendantOf(pMailboxId.MailboxName))
                {
                    var lChildMailboxName = lMailboxName.GetLineageMemberThatIsChildOf(pMailboxId.MailboxName);
                    if (!pAllExistentChildMailboxNames.Contains(lChildMailboxName)) ClearCachedItems(new cMailboxId(pMailboxId.AccountId, lMailboxName), lContext);
                }
            }
        }

        internal void Reconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, HashSet<cMailboxName> pAllExistentChildMailboxNames, HashSet<cMailboxName> pAllSelectableChildMailboxNames, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(Reconcile), pAccountId, pPrefix, pNotPrefixedWith);

            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pNotPrefixedWith == null) throw new ArgumentNullException(nameof(pNotPrefixedWith));
            if (pAllExistentChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllExistentChildMailboxNames));
            if (pAllSelectableChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllSelectableChildMailboxNames));

            var lMailboxNames = YGetMailboxNames(pAccountId, lContext);

            foreach (var lMailboxName in lMailboxNames)
            {
                if (lMailboxName.IsFirstLineageMemberPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    if (!pAllSelectableChildMailboxNames.Contains(lMailboxName)) ClearCachedItems(new cMailboxId(pAccountId, lMailboxName), lContext);
                }
                else if (lMailboxName.IsPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    var lFirstMailboxName = lMailboxName.GetFirstLineageMemberPrefixedWith(pPrefix);
                    if (!pAllExistentChildMailboxNames.Contains(lFirstMailboxName)) ClearCachedItems(new cMailboxId(pAccountId, lMailboxName), lContext);
                }
            }
        }
    }
}
