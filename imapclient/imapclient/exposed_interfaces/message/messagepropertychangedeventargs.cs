using System;
using System.ComponentModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMessagePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public readonly iMessageHandle Handle;

        public cMessagePropertyChangedEventArgs(iMessageHandle pHandle, string pPropertyName) : base(pPropertyName)
        {
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMessagePropertyChangedEventArgs)}({Handle},{PropertyName})";
    }
}