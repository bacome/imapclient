using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cPersistentCache
    {
        public abstract HashSet<cUID> GetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);
        public abstract void MessageExpunged(cMailboxId pMailboxId, cUID pUID, cTrace.cContext pParentContext);
        public abstract void MessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext);
        public abstract void SetMailboxUIDValidity(cMailboxId pMailboxId, long pUIDValidity, cTrace.cContext pParentContext);

        public virtual void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Copy), pSourceMailboxId, pDestinationMailboxName, pFeedback);
        }

        protected abstract HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext);

        protected virtual void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(YRename), pMailboxId, pMailboxName);
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
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ZRenameInbox), pMailboxId, pUIDValidity);

            if (pUIDValidity == 0)
            {
                try { SetMailboxUIDValidity(pMailboxId, -1, lContext); }
                catch (Exception e) { lContext.TraceException(nameof(SetMailboxUIDValidity), e); }
            }
            else
            {
                HashSet<cUID> lUIDs;

                try { lUIDs = GetUIDs(pMailboxId, pUIDValidity, lContext); }
                catch (Exception e)
                {
                    lContext.TraceException(nameof(GetUIDs), e);
                    return;
                }

                try { MessagesExpunged(pMailboxId, lUIDs, lContext); }
                catch (Exception e) { lContext.TraceException(nameof(MessageExpunged), e); }
            }
        }

        private void ZRenameNonInbox(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(ZRenameNonInbox), pMailboxId, pMailboxName);

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
            catch (Exception e) { lContext.TraceException($"{nameof(SetMailboxUIDValidity)}({pMailboxId})", e); }

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
                catch (Exception e) { lContext.TraceException($"{nameof(SetMailboxUIDValidity)}({lOldMailboxId})", e); }
            }
        }

        public void Reconcile(cMailboxId pMailboxId, HashSet<cMailboxName> pAllExistentChildMailboxNames, HashSet<cMailboxName> pAllSelectableChildMailboxNames, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Reconcile), pMailboxId);

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
                        catch (Exception e) { lContext.TraceException(nameof(SetMailboxUIDValidity), e); }
                    }
                }
                else if (lMailboxName.IsDescendantOf(pMailboxId.MailboxName))
                {
                    var lChildMailboxName = lMailboxName.GetLineageMemberThatIsChildOf(pMailboxId.MailboxName);
                    
                    if (!pAllExistentChildMailboxNames.Contains(lChildMailboxName))
                    {
                        try { SetMailboxUIDValidity(new cMailboxId(pMailboxId.AccountId, lMailboxName), -1, lContext); }
                        catch (Exception e) { lContext.TraceException(nameof(SetMailboxUIDValidity), e); }
                    }
                }
            }
        }

        public void Reconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, HashSet<cMailboxName> pAllExistentChildMailboxNames, HashSet<cMailboxName> pAllSelectableChildMailboxNames, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cPersistentCache), nameof(Reconcile), pAccountId, pPrefix, pNotPrefixedWith);

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
                        catch (Exception e) { lContext.TraceException(nameof(SetMailboxUIDValidity), e); }
                    }
                }
                else if (lMailboxName.IsPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    var lFirstMailboxName = lMailboxName.GetFirstLineageMemberPrefixedWith(pPrefix);
                    
                    if (!pAllExistentChildMailboxNames.Contains(lFirstMailboxName))
                    {
                        try { SetMailboxUIDValidity(new cMailboxId(pAccountId, lMailboxName), -1, lContext); }
                        catch (Exception e) { lContext.TraceException(nameof(SetMailboxUIDValidity), e); }
                    }
                }
            }
        }
    }
}
