using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMessageDeliveryEventArgs : EventArgs
    {
        public readonly cHandleList Handles;
        public cMessageDeliveryEventArgs(cHandleList pHandles) { Handles = pHandles; }
        public override string ToString() => $"{nameof(cMessageDeliveryEventArgs)}({Handles})";
    }

    public class cMailboxMessageDeliveryEventArgs : cMessageDeliveryEventArgs
    {
        public readonly cMailboxId MailboxId;
        public readonly iMailboxHandle Handle;

        public cMailboxMessageDeliveryEventArgs(cMailboxId pMailboxId, iMailboxHandle pHandle, cHandleList pHandles) : base(pHandles)
        {
            MailboxId = pMailboxId;
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMailboxMessageDeliveryEventArgs)}({MailboxId},{Handle},{Handles})";
    }
}