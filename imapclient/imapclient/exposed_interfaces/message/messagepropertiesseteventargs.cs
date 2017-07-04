using System;

namespace work.bacome.imapclient
{
    public class cAttributesSetEventArgs : EventArgs
    {
        public readonly fFetchAttributes AttributesSet;
        public cAttributesSetEventArgs(fFetchAttributes pAttributesSet) { AttributesSet = pAttributesSet; }
        public override string ToString() => $"{nameof(cAttributesSetEventArgs)}({AttributesSet})";
    }

    public class cMessageAttributesSetEventArgs : cAttributesSetEventArgs
    {
        public readonly cMailboxId MailboxId;
        public readonly iMessageHandle Handle;

        public cMessageAttributesSetEventArgs(cMailboxId pMailboxId, iMessageHandle pHandle, fFetchAttributes pAttributesSet) : base(pAttributesSet)
        {
            MailboxId = pMailboxId;
            Handle = pHandle;
        }

        public override string ToString() => $"{nameof(cMessageAttributesSetEventArgs)}({MailboxId},{Handle},{AttributesSet})";
    }
}