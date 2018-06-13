using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private void ZCacheIntegrationMessageExpunged(cMailboxId pMailboxId, cUID pUID, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCacheIntegrationMessageExpunged), pMailboxId, pUID);

            try { HeaderCache?.MessageExpunged(pMailboxId, pUID, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { SectionCache.MessageExpunged(pMailboxId, pUID, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }
        }

        private void ZCacheIntegrationMessagesExpunged(cMailboxId pMailboxId, IEnumerable<cUID> pUIDs, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCacheIntegrationMessagesExpunged), pMailboxId);
            
            try { HeaderCache?.MessagesExpunged(pMailboxId, pUIDs, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { SectionCache.MessagesExpunged(pMailboxId, pUIDs, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }
        }

        private void ZCacheIntegrationSetMailboxUIDValidity(cMailboxId pMailboxId, long pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCacheIntegrationSetMailboxUIDValidity), pMailboxId, pUIDValidity);

            try { HeaderCache?.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { SectionCache.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }
        }

        private void ZCacheIntegrationCopy(cMailboxId pSourceMailboxId, cMailboxName pDestinationMailboxName, cCopyFeedback pFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCacheIntegrationCopy), pSourceMailboxId, pDestinationMailboxName, pFeedback);
            
            try { HeaderCache?.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, pParentContext); }
            catch (Exception e) { lContext.TraceException("header cache threw", e); }

            try { SectionCache.Copy(pSourceMailboxId, pDestinationMailboxName, pFeedback, pParentContext); }
            catch (Exception e) { lContext.TraceException("section cache threw", e); }
        }

        private void ZCacheIntegrationRename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            HeaderCache?.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
            SectionCache.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
        }

        private void ZCacheIntegrationReconcile(cMailboxId pMailboxId, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));
            ZZCacheIntegrationReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);
            HeaderCache?.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            SectionCache.Reconcile(pMailboxId, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
        }

        private void ZCacheIntegrationReconcile(cAccountId pAccountId, string pPrefix, cStrings pNotPrefixedWith, IEnumerable<iMailboxHandle> pAllChildMailboxHandles, cTrace.cContext pParentContext)
        {
            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pNotPrefixedWith == null) throw new ArgumentNullException(nameof(pNotPrefixedWith));
            if (pAllChildMailboxHandles == null) throw new ArgumentNullException(nameof(pAllChildMailboxHandles));
            ZZCacheIntegrationReconcile(pAllChildMailboxHandles, out var lExistentChildMailboxNames, out var lSelectableChildMailboxNames);
            HeaderCache?.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
            SectionCache.Reconcile(pAccountId, pPrefix, pNotPrefixedWith, lExistentChildMailboxNames, lSelectableChildMailboxNames, pParentContext);
        }

        private void ZZCacheIntegrationReconcile(IEnumerable<iMailboxHandle> pMailboxHandles, out HashSet<cMailboxName> rExistentMailboxNames, out HashSet<cMailboxName> rSelectableMailboxNames)
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

        private HashSet<cUID> ZCacheIntegrationGetUIDs(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCacheIntegrationGetUIDs), pMailboxId, pUIDValidity);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            var lUIDs = new HashSet<cUID>();

            try { lUIDs.UnionWith(SectionCache.GetUIDs(pMailboxId, pUIDValidity, lContext)); }
            catch (Exception e) { lContext.TraceException(nameof(SectionCache), e); }

            if (HeaderCache != null)
            {
                try { lUIDs.UnionWith(HeaderCache.GetUIDs(pMailboxId, pUIDValidity, lContext)); }
                catch (Exception e) { lContext.TraceException(nameof(HeaderCache), e); }
            }

            return lUIDs;
        }
    }
}