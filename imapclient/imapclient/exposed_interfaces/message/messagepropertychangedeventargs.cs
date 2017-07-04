using System;
using System.ComponentModel;

namespace work.bacome.imapclient
{
    public class cMessagePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public readonly cMailboxId MailboxId;
        public readonly iMessageHandle Handle;

        public cMessagePropertyChangedEventArgs(cMailboxId pMailboxId, iMessageHandle pHandle, string pPropertyName) : base(pPropertyName)
        {
            MailboxId = pMailboxId;
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMessagePropertyChangedEventArgs)}({MailboxId},{Handle},{PropertyName})";
    }
}