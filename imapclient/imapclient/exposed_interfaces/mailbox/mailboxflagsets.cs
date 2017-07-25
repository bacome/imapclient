using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxFlagSets
    {
        subscribed = 1 << 0,
        children = 1 << 1,
        specialuse = 1 << 2
    }
}