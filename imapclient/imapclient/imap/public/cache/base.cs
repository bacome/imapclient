using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cCache
    {
        public abstract void MessageExpunged(cMessageUID pMessageUID, cTrace.cContext pParentContext);
        public abstract void MessagesExpunged(IEnumerable<cMessageUID> pMessageUIDs, cTrace.cContext pParentContext);
        public abstract void SetMailboxUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);
        public abstract void Copy(IEnumerable<cMessageUID> pMessageUIDs, cMailboxName pMailboxName, cTrace.cContext pParentContext);

        protected abstract HashSet<uint> YGetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext);

        ;?; // replace with the below and the required processing
        protected abstract HashSet<cMailboxName> YGetDescendants(cMailboxId pMailboxId, cTrace.cContext pParentContext);
        protected abstract HashSet<cMailboxName> YGetChildren(cMailboxId pMailboxId, cTrace.cContext pParentContext);
        protected abstract HashSet<cMailboxName> YGetTopLevel(cAccountId pAccountId, cNamespaceName pNamespaceName, IEnumerable<cNamespaceName> pNamespaceNames, cTrace.cContext pParentContext);
        ;?;

        protected abstract HashSet<cMailboxName> YGetMailboxNames(cAccountId pAccountId, cTrace.cContext pParentContext);


        protected abstract void YRename(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext);

        internal void Rename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCache), nameof(Rename), pMailboxId, pUIDValidity);
            if (pMailboxId.MailboxName.IsInbox) ZRenameInbox(pMailboxId, pUIDValidity, lContext);
            else ZRenameNonInbox(pMailboxId, pMailboxName, lContext);
        }

        private void ZRenameInbox(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCache), nameof(ZRenameInbox), pMailboxId, pUIDValidity);
            var lUIDs = YGetUIDs(pMailboxId, pUIDValidity, lContext);
            List<cMessageUID> lMessageUIDs = new List<cMessageUID>();
            foreach (var lUID in lUIDs) lMessageUIDs.Add(new cMessageUID(pMailboxId, new cUID(pUIDValidity, lUID)));
            MessagesExpunged(lMessageUIDs, lContext);
        }

        private void ZRenameNonInbox(cMailboxId pMailboxId, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCache), nameof(ZRenameNonInbox), pMailboxId, pMailboxName);

            YRename(pMailboxId, pMailboxName, lContext);
            SetMailboxUIDValidity(pMailboxId, 0, lContext);

            int lStartIndex = pMailboxId.MailboxName.Path.Length + 1;

            foreach (var lMailboxName in YGetDescendants(pMailboxId, lContext))
            {
                var lNewPath = pMailboxName.Path + pMailboxName.Delimiter + lMailboxName.Path.Substring(lStartIndex);
                var lNewMailboxName = new cMailboxName(lNewPath, pMailboxName.Delimiter);
                var lOldMailboxId = new cMailboxId(pMailboxId.AccountId, lMailboxName);
                YRename(lOldMailboxId, lNewMailboxName, lContext);
                SetMailboxUIDValidity(lOldMailboxId, 0, lContext);
            }
        }

        ;?; // reconcile, mailboxes, messages
    }
}
