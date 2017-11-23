using System;
using System.ComponentModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries mailbox property change event data.
    /// </summary>
    /// <seealso cref="cIMAPClient.MailboxPropertyChanged"/>
    public class cMailboxPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /**<summary>The mailbox that changed.</summary>*/
        public readonly iMailboxHandle MailboxHandle;

        internal cMailboxPropertyChangedEventArgs(iMailboxHandle pMailboxHandle, string pPropertyName) : base(pPropertyName)
        {
            MailboxHandle = pMailboxHandle;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxPropertyChangedEventArgs)}({MailboxHandle},{PropertyName})";
    }
}