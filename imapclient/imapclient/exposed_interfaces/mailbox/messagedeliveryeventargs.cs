using System;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries message delivery event data.
    /// </summary>
    /// <seealso cref="cMailbox.MessageDelivery"/>
    public class cMessageDeliveryEventArgs : EventArgs
    {
        /**<summary>The internal message cache items that were delivered.</summary>*/
        public readonly cMessageHandleList Handles;
        internal cMessageDeliveryEventArgs(cMessageHandleList pHandles) { Handles = pHandles; }
        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMessageDeliveryEventArgs)}({Handles})";
    }

    /// <summary>
    /// Carries message delivery event data.
    /// </summary>
    /// <seealso cref="cIMAPClient.MailboxMessageDelivery"/>
    public class cMailboxMessageDeliveryEventArgs : cMessageDeliveryEventArgs
    {
        /**<summary>The internal mailbox cache item that the messages were delivered to.</summary>*/
        public readonly iMailboxHandle Handle;

        internal cMailboxMessageDeliveryEventArgs(iMailboxHandle pHandle, cMessageHandleList pHandles) : base(pHandles)
        {
            Handle = pHandle;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMailboxMessageDeliveryEventArgs)}({Handle},{Handles})";
    }
}