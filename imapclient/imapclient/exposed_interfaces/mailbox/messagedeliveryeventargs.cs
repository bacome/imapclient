using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMessageDeliveryEventArgs : EventArgs
    {
        public readonly cMessageHandleList Handles;
        public cMessageDeliveryEventArgs(cMessageHandleList pHandles) { Handles = pHandles; }
        public override string ToString() => $"{nameof(cMessageDeliveryEventArgs)}({Handles})";
    }

    public class cMailboxMessageDeliveryEventArgs : cMessageDeliveryEventArgs
    {
        public readonly iMailboxHandle Handle;

        public cMailboxMessageDeliveryEventArgs(iMailboxHandle pHandle, cMessageHandleList pHandles) : base(pHandles)
        {
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMailboxMessageDeliveryEventArgs)}({Handle},{Handles})";
    }
}