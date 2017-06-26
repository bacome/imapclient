using System;

namespace work.bacome.imapclient
{
    public class cMessageExpungedEventArgs : EventArgs
    {
        public readonly cMailboxId MailboxId;
        public readonly iMessageHandle Handle;

        public cMessageExpungedEventArgs(cMailboxId pMailboxId, iMessageHandle pHandle)
        {
            MailboxId = pMailboxId;
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMessageExpungedEventArgs)}({MailboxId},{Handle})";
    }
}