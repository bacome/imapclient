using System;
using System.ComponentModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries message property change event data.
    /// </summary>
    /// <seealso cref="cIMAPClient.MessagePropertyChanged"/>
    public class cMessagePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /**<summary>The message that changed.</summary>*/
        public readonly iMessageHandle MessageHandle;

        internal cMessagePropertyChangedEventArgs(iMessageHandle pMessageHandle, string pPropertyName) : base(pPropertyName)
        {
            MessageHandle = pMessageHandle;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cMessagePropertyChangedEventArgs)}({MessageHandle},{PropertyName})";
    }
}