using System;
using System.ComponentModel;
using work.bacome.apidocumentation;
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
        public readonly iMailboxHandle Handle;

        internal cMailboxPropertyChangedEventArgs(iMailboxHandle pHandle, string pPropertyName) : base(pPropertyName)
        {
            Handle = pHandle;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMailboxPropertyChangedEventArgs)}({Handle},{PropertyName})";
    }
}