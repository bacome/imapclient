using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cPersistentCacheComponent
    {
        protected internal abstract uint GetUIDValidity(cMailboxId pMailboxId, cTrace.cContext pParentContext);
        protected internal abstract ulong GetHighestModSeq(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);
        protected internal abstract HashSet<cUID> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);

        protected internal abstract void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext);

        protected internal abstract void SetUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);
        protected internal abstract void SetHighestModSeq(cMailboxId pMailboxId, ulong pHighestModSeq, cTrace.cContext pParentContext);
        protected internal abstract void ClearCachedItems(cMailboxId pMailboxId, cTrace.cContext pParentContext);





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
        }

        internal void Rename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));
            if (pMailboxId.MailboxName.IsInbox) ZRenameInbox(pMailboxId, pUIDValidity, pParentContext);
            else ZRenameNonInbox(pMailboxId, pMailboxName, pParentContext);
        }

        private void ZRenameInbox(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(ZRenameInbox), pMailboxId, pUIDValidity);

            if (pUIDValidity == 0)
            {
                try { SetMailboxUIDValidity(pMailboxId, -1, lContext); }
                catch (Exception e) { lContext.TraceException(e); }
            }
            else
            {
                HashSet<cUID> lUIDs;

                try { lUIDs = GetUIDs(pMailboxId, pUIDValidity, lContext); }
                catch (Exception e)
                {
                    lContext.TraceException(e);
                    return;
                }

                ;?; // use messages expunged
                foreach (var lUID in lUIDs) 
                {
                    try { MessageExpunged(pMailboxId, lUID, lContext); }
                    catch (Exception e) { lContext.TraceException(e); }
                }
            }
        }

        private void ZRenameNonInbox(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(ZRenameNonInbox), pMailboxId, pMailboxName);

            HashSet<cMailboxName> lMailboxNames;

            try { lMailboxNames = YGetMailboxNames(pMailboxId.AccountId, lContext); }
            catch (Exception e) 
            {
                lContext.TraceException(nameof(YGetMailboxNames), e);
                return;
            }

            try { YRename(pMailboxId, pMailboxName, lContext); }
            catch (Exception e) { lContext.TraceException($"{nameof(YRename)}({pMailboxId})", e); }

            try { SetMailboxUIDValidity(pMailboxId, -1, lContext); }
            catch (Exception e) { lContext.TraceException(e); }

            int lStartIndex = pMailboxId.MailboxName.GetDescendantPathPrefix().Length;

            foreach (var lMailboxName in lMailboxNames)
            {
                if (!lMailboxName.IsDescendantOf(pMailboxId.MailboxName)) continue;
                var lNewPath = pMailboxName.GetDescendantPathPrefix() + lMailboxName.Path.Substring(lStartIndex);
                var lNewMailboxName = new cMailboxName(lNewPath, pMailboxName.Delimiter);
                var lOldMailboxId = new cMailboxId(pMailboxId.AccountId, lMailboxName);

                try { YRename(lOldMailboxId, lNewMailboxName, lContext); }
                catch (Exception e) { lContext.TraceException($"{nameof(YRename)}({lOldMailboxId})", e); }

                try { SetMailboxUIDValidity(lOldMailboxId, -1, lContext); }
                catch (Exception e) { lContext.TraceException(e); }
            }
        }

        internal void Reconcile(cMailboxId pMailboxId, HashSet<cMailboxName> pAllExistentChildMailboxNames, HashSet<cMailboxName> pAllSelectableChildMailboxNames, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCacheComponent), nameof(Reconcile), pMailboxId);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllExistentChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllExistentChildMailboxNames));
            if (pAllSelectableChildMailboxNames == null) throw new ArgumentNullException(nameof(pAllSelectableChildMailboxNames));

            HashSet<cMailboxName> lMailboxNames;

            try { lMailboxNames = YGetMailboxNames(pMailboxId.AccountId, lContext); }
            catch (Exception e)
            {
                lContext.TraceException(nameof(YGetMailboxNames), e);
                return;
            }

            foreach (var lMailboxName in lMailboxNames)
            {
                if (lMailboxName.IsChildOf(pMailboxId.MailboxName))
                {
                    if (!pAllSelectableChildMailboxNames.Contains(lMailboxName))
                    {
                        try { SetMailboxUIDValidity(new cMailboxId(pMailboxId.AccountId, lMailboxName), -1, lContext); }
                        catch (Exception e) { lContext.TraceException(e); }
                    }
                }
                else if (lMailboxName.IsDescendantOf(pMailboxId.MailboxName))
                {
                    var lChildMailboxName = lMailboxName.GetLineageMemberThatIsChildOf(pMailboxId.MailboxName);

                    if (!pAllExistentChildMailboxNames.Contains(lChildMailboxName))
                    {
                        try { SetMailboxUIDValidity(new cMailboxId(pMailboxId.AccountId, lMailboxName), -1, lContext); }
                        catch (Exception e) { lContext.TraceException(e); }
                    }
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

            HashSet<cMailboxName> lMailboxNames;

            try { lMailboxNames = YGetMailboxNames(pAccountId, lContext); }
            catch (Exception e)
            {
                lContext.TraceException(nameof(YGetMailboxNames), e);
                return;
            }

            foreach (var lMailboxName in lMailboxNames)
            {
                if (lMailboxName.IsFirstLineageMemberPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    if (!pAllSelectableChildMailboxNames.Contains(lMailboxName))
                    {
                        try { SetMailboxUIDValidity(new cMailboxId(pAccountId, lMailboxName), -1, lContext); }
                        catch (Exception e) { lContext.TraceException(e); }
                    }
                }
                else if (lMailboxName.IsPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    var lFirstMailboxName = lMailboxName.GetFirstLineageMemberPrefixedWith(pPrefix);

                    if (!pAllExistentChildMailboxNames.Contains(lFirstMailboxName))
                    {
                        try { SetMailboxUIDValidity(new cMailboxId(pAccountId, lMailboxName), -1, lContext); }
                        catch (Exception e) { lContext.TraceException(e); }
                    }
                }
            }
        }
    }
}
