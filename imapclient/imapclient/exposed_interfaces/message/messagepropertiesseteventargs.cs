using System;

namespace work.bacome.imapclient
{
    public class cPropertiesSetEventArgs : EventArgs
    {
        public readonly fMessageProperties Set;
        public cPropertiesSetEventArgs(fMessageProperties pSet) { Set = pSet; }
        public override string ToString() => $"{nameof(cPropertiesSetEventArgs)}({Set})";
    }

    public class cMessagePropertiesSetEventArgs : cPropertiesSetEventArgs
    {
        public readonly cMailboxId MailboxId;
        public readonly iMessageHandle Handle;

        public cMessagePropertiesSetEventArgs(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pSet) : base(pSet)
        {
            MailboxId = pMailboxId;
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMessagePropertiesSetEventArgs)}({MailboxId},{Handle.HandleString},{Set})";
    }
}