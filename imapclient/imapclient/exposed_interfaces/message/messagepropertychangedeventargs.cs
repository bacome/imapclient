using System;
using System.ComponentModel;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries message property change event data.
    /// </summary>
    /// <seealso cref="cIMAPClient.MessagePropertyChanged"/>
    public class cMessagePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /**<summary>The internal message cache item that changed.</summary>*/
        public readonly iMessageHandle Handle;

        internal cMessagePropertyChangedEventArgs(iMessageHandle pHandle, string pPropertyName) : base(pPropertyName)
        {
            Handle = pHandle;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMessagePropertyChangedEventArgs)}({Handle},{PropertyName})";
    }
}