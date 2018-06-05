using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        // if/when the attribute cache is implemented; these will also call attribute cache 
        private void ZAddExpungedMessage(iMessageHandle pMessageHandle, cTrace.cContext pParentContext)
        {
            SectionCache.AddExpungedMessage(pMessageHandle, pParentContext);
        }

        private void ZAddMailboxUIDValidity(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext)
        {
            SectionCache.AddMailboxUIDValidity(pMailboxHandle, pParentContext);
        }
    }
}