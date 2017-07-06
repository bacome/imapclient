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
}
