using System;

namespace work.bacome.imapclient
{
    public class cPropertiesSetEventArgs : EventArgs
    {
        public readonly fMessageProperties PropertiesSet;
        public cPropertiesSetEventArgs(fMessageProperties pPropertiesSet) { PropertiesSet = pPropertiesSet; }
        public override string ToString() => $"{nameof(cPropertiesSetEventArgs)}({PropertiesSet})";
    }

    public class cMessagePropertiesSetEventArgs : cPropertiesSetEventArgs
    {
        public readonly cMailboxId MailboxId;
        public readonly iMessageHandle Handle;

        public cMessagePropertiesSetEventArgs(cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pPropertiesSet) : base(pPropertiesSet)
        {
            MailboxId = pMailboxId;
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMessagePropertiesSetEventArgs)}({MailboxId},{Handle},{PropertiesSet})";
    }
}