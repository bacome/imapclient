using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        // when the attribute cache is implemented; these will also call attribute cache 

        private void ZCacheIntegrationMessageExpunged(cMessageUID pMessageUID, cTrace.cContext pParentContext)
        {
            SectionCache.MessageExpunged(pMessageUID, pParentContext);
        }

        private void ZCacheIntegrationMessagesExpunged(IList<cMessageUID> pMessageUIDs, cTrace.cContext pParentContext)
        {
            SectionCache.MessagesExpunged(pMessageUIDs, pParentContext);
        }

        private void ZCacheIntegrationSetMailboxUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            SectionCache.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext);
        }

        private void ZCacheIntegrationCopy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            SectionCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, pParentContext);
        }

        private void ZCacheIntegrationRename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            SectionCache.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
        }

        private void ZCacheIntegrationReconcile(cMailboxId pMailboxId, IEnumerable<cMailbox> pAllChildMailboxes, cTrace.cContext pParentContext)
        {
            ZZCacheIntegrationReconcile(pAllChildMailboxes, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);
            SectionCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
        }

        private void ZCacheIntegrationReconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, IEnumerable<cMailbox> pAllChildMailboxes, cTrace.cContext pParentContext)
        {
            ZZCacheIntegrationReconcile(pAllChildMailboxes, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);
            SectionCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
        }

        private void ZZCacheIntegrationReconcile(IEnumerable<cMailbox> pChildMailboxes, out HashSet<cMailboxName> rExistentChildMailboxNames, out HashSet<cMailboxName> rSelectableChildMailboxNames)
        {
            rExistentChildMailboxNames = new HashSet<cMailboxName>();
            rSelectableChildMailboxNames = new HashSet<cMailboxName>();

            foreach (var lMailbox in pChildMailboxes)
            {
                var lMailboxHandle = lMailbox.MailboxHandle;
                if (lMailboxHandle.Exists != true) continue;
                rExistentChildMailboxNames.Add(lMailbox.MailboxHandle.MailboxName);
                if (lMailboxHandle.ListFlags?.CanSelect != true) continue;
                rSelectableChildMailboxNames.Add(lMailbox.MailboxHandle.MailboxName);
            }
        }
    }
}