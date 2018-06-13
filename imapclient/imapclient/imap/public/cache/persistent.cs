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
        public abstract void Copy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext);

        protected abstract HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext);
        protected abstract void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext);

        protected virtual bool YCanRename => false;

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

            if (!YCanRename) return;

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

            ;?;
            foreach (var lMailboxName in YGetMailboxNames(pMailboxId.AccountId, lContext))
            {
                if (lMailboxName.IsChildOf(pMailboxId.MailboxName))
                {
                    ;?;
                    if (!pAllSelectableChildMailboxNames.Contains(lMailboxName)) SetMailboxUIDValidity(new cMailboxId(pMailboxId.AccountId, lMailboxName), -1, lContext);
                }
                else if (lMailboxName.IsDescendantOf(pMailboxId.MailboxName))
                {
                    var lChildMailboxName = lMailboxName.GetLineageMemberThatIsChildOf(pMailboxId.MailboxName);
                    ;?;
                    if (!pAllExistentChildMailboxNames.Contains(lChildMailboxName)) SetMailboxUIDValidity(new cMailboxId(pMailboxId.AccountId, lMailboxName), -1, lContext);
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

            ;?;
            foreach (var lMailboxName in YGetMailboxNames(pAccountId, lContext))
            {
                if (lMailboxName.IsFirstLineageMemberPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    ;?;
                    if (!pAllSelectableChildMailboxNames.Contains(lMailboxName)) SetMailboxUIDValidity(new cMailboxId(pAccountId, lMailboxName), -1, lContext);
                }
                else if (lMailboxName.IsPrefixedWith(pPrefix, pNotPrefixedWith))
                {
                    var lFirstMailboxName = lMailboxName.GetFirstLineageMemberPrefixedWith(pPrefix);
                    ;?;
                    if (!pAllExistentChildMailboxNames.Contains(lFirstMailboxName)) SetMailboxUIDValidity(new cMailboxId(pAccountId, lMailboxName), -1, lContext);
                }
            }
        }
    }
}
