using System;
using System.Collections.Generic;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        // when the attribute cache is implemented; these will also call attribute cache 

        private void ZMessageExpunged(cMessageUID pMessageUID, cTrace.cContext pParentContext)
        {
            SectionCache.MessageExpunged(pMessageUID, pParentContext);
        }

        private void ZMessagesExpunged(IList<cMessageUID> pMessageUIDs, cTrace.cContext pParentContext)
        {
            SectionCache.MessagesExpunged(pMessageUIDs, pParentContext);
        }

        private void ZSetMailboxUIDValidity(cMailboxId pMailboxId, uint pUIDValidity, cTrace.cContext pParentContext)
        {
            SectionCache.SetMailboxUIDValidity(pMailboxId, pUIDValidity, pParentContext);
        }

        private void ZCopy(IEnumerable<cMessageUID> pMessageUIDs, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            SectionCache.Copy(pMessageUIDs, pMailboxName, pParentContext);
        }

        private void ZRename(cMailboxId pMailboxId, uint pUIDValidity, cMailboxName pMailboxName, cTrace.cContext pParentContext)
        {
            SectionCache.Rename(pMailboxId, pUIDValidity, pMailboxName, pParentContext);
        }
    }
}