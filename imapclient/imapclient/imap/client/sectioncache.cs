using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private void ZExpunged(iMessageHandle pMessageHandle, cTrace.cContext pParentContext) => SectionCache.Expunged(pMessageHandle, pParentContext);
        private void ZUIDValidityDiscovered(iMailboxHandle pMailboxHandle, cTrace.cContext pParentContext) => SectionCache.UIDValidityDiscovered(pMailboxHandle, pParentContext);
    }
}