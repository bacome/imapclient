using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxFlagSets
    {
        // this lists the properties that the mailbox listing must have filled in accurately
        clientdefault = 1,
        rfc3501 = 1 << 1, // rfc 3501 flags
        children = 1 << 2, // may cause multiple list commands to be issued
        subscribed = 1 << 3,
        subscribedchildren = 1 << 4,
        local = 1 << 5,
        specialuse = 1 << 6, // if list-extended is supported, may cause specialuse to be requested
        all = 0b1111110
    }
}