using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries message delivery event data.
    /// </summary>
    /// <seealso cref="cMailbox.MessageDelivery"/>
    public class cMessageDeliveryEventArgs : EventArgs
    {
        /**<summary>The messages that were delivered.</summary>*/
        public readonly cMessageHandleList Handles;
        internal cMessageDeliveryEventArgs(cMessageHandleList pHandles) { Handles = pHandles; }
        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMessageDeliveryEventArgs)}({Handles})";
    }

    /// <summary>
    /// Carries message delivery event data.
    /// </summary>
    /// <seealso cref="cIMAPClient.MailboxMessageDelivery"/>
    public class cMailboxMessageDeliveryEventArgs : cMessageDeliveryEventArgs
    {
        /**<summary>The mailbox that the messages were delivered to.</summary>*/
        public readonly iMailboxHandle Handle;

        internal cMailboxMessageDeliveryEventArgs(iMailboxHandle pHandle, cMessageHandleList pHandles) : base(pHandles)
        {
            Handle = pHandle;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxMessageDeliveryEventArgs)}({Handle},{Handles})";
    }
}