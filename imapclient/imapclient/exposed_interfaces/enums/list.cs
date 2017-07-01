using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxTypes
    {
        // this lists the types of mailboxes that can be returned
        clientdefault = 1,
        normal = 1 << 1,
        subscribed = 1 << 2,
        remote = 1 << 3,
        all = 0b1110
    }

    [Flags]
    public enum fMailboxFlagSets
    {
        // this lists the flag sets that the mailbox listing must have filled in accurately
        clientdefault = 1,
        rfc3501 = 1 << 1, // rfc 3501 flags
        children = 1 << 2, // may cause multiple list commands to be issued
        subscribed = 1 << 3,
        subscribedchildren = 1 << 4,
        specialuse = 1 << 5, // if list-extended is supported, may cause specialuse to be requested
        all = 0b111110
    }
}
