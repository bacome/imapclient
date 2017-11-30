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
        public readonly cMessageHandleList MessageHandles;
        internal cMessageDeliveryEventArgs(cMessageHandleList pMessageHandles) { MessageHandles = pMessageHandles; }
        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMessageDeliveryEventArgs)}({MessageHandles})";
    }

    /// <summary>
    /// Carries message delivery event data.
    /// </summary>
    /// <seealso cref="cIMAPClient.MailboxMessageDelivery"/>
    public class cMailboxMessageDeliveryEventArgs : cMessageDeliveryEventArgs
    {
        /**<summary>The mailbox that the messages were delivered to.</summary>*/
        public readonly iMailboxHandle MailboxHandle;

        internal cMailboxMessageDeliveryEventArgs(iMailboxHandle pMailboxHandle, cMessageHandleList pMessageHandles) : base(pMessageHandles)
        {
            MailboxHandle = pMailboxHandle;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxMessageDeliveryEventArgs)}({MailboxHandle},{MessageHandles})";
    }
}