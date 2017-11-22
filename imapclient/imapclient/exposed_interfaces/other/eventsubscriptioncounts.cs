using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains the counts of the subscriptions to various <see cref="cIMAPClient"/> events.
    /// </summary>
    /// <seealso cref="cIMAPClient.EventSubscriptionCounts"/>
    public struct sEventSubscriptionCounts
    {
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.PropertyChanged"/> event.</summary>*/
        public int PropertyChangedSubscriptionCount;
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.ResponseText"/> event.</summary>*/
        public int ResponseTextSubscriptionCount;
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.NetworkReceive"/> event.</summary>*/
        public int NetworkReceiveSubscriptionCount;
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.NetworkSend"/> event.</summary>*/
        public int NetworkSendSubscriptionCount;
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.MailboxPropertyChanged"/> event. The count includes one for each <see cref="cMailbox"/> with some <see cref="cMailbox.PropertyChanged"/> subscriptions.</summary>*/
        public int MailboxPropertyChangedSubscriptionCount;
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.MailboxMessageDelivery"/> event. The count includes one for each <see cref="cMailbox"/> with some <see cref="cMailbox.MessageDelivery"/> subscriptions.</summary>*/
        public int MailboxMessageDeliverySubscriptionCount;
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.MessagePropertyChanged"/> event. The count includes one for each <see cref="cMessage"/> with some <see cref="cMessage.PropertyChanged"/> subscriptions.</summary>*/
        public int MessagePropertyChangedSubscriptionCount;
        /**<summary>The count of subscriptions to the <see cref="cIMAPClient.CallbackException"/> event.</summary>*/
        public int CallbackExceptionSubscriptionCount;
        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cIMAPClient.PropertyChanged)}:{PropertyChangedSubscriptionCount} {nameof(cIMAPClient.ResponseText)}:{ResponseTextSubscriptionCount} {nameof(cIMAPClient.NetworkReceive)}:{NetworkReceiveSubscriptionCount} {nameof(cIMAPClient.NetworkSend)}:{NetworkSendSubscriptionCount} {nameof(cIMAPClient.MailboxPropertyChanged)}:{MailboxPropertyChangedSubscriptionCount} {nameof(cIMAPClient.MailboxMessageDelivery)}:{MailboxMessageDeliverySubscriptionCount} {nameof(cIMAPClient.MessagePropertyChanged)}:{MessagePropertyChangedSubscriptionCount} {nameof(cIMAPClient.CallbackException)}:{CallbackExceptionSubscriptionCount}";
    }
}