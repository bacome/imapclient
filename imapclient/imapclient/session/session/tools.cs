using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static void ZMessagePropertiesChanged(cEventSynchroniser pEventSynchroniser, cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZMailboxPropertiesChanged), pMailboxId, pProperties);

                if (pEventSynchroniser == null) throw new ArgumentNullException(nameof(pEventSynchroniser));
                if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                if (pProperties == 0 || !pEventSynchroniser.AreMessagePropertyChangedSubscriptions) return;

            }
        }
    }
}