using System;

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
        public cMailboxMessageDeliveryEventArgs(cMailboxId pMailboxId, cHandleList pHandles) : base(pHandles) { MailboxId = pMailboxId; }
        public override string ToString() => $"{nameof(cMailboxMessageDeliveryEventArgs)}({MailboxId},{Handles})";
    }
}