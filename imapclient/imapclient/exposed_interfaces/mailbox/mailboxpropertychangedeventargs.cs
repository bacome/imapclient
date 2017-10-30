using System;
using System.ComponentModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailboxPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public readonly iMailboxHandle Handle;

        public cMailboxPropertyChangedEventArgs(iMailboxHandle pHandle, string pPropertyName) : base(pPropertyName)
        {
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMailboxPropertyChangedEventArgs)}({Handle},{PropertyName})";
    }
}