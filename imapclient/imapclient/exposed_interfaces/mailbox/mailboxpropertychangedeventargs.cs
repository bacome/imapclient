using System;
using System.ComponentModel;

namespace work.bacome.imapclient
{
    public class cMailboxPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public readonly cMailboxId MailboxId;
        public cMailboxPropertyChangedEventArgs(cMailboxId pMailboxId, string pPropertyName) : base(pPropertyName) { MailboxId = pMailboxId; }
        public override string ToString() => $"{nameof(cMailboxPropertyChangedEventArgs)}({MailboxId},{PropertyName})";
    }
}