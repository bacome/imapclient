using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxTypes
    {
        clientdefault = 1 << 0,
        subscribedonly = 1 << 1,
        remote = 1 << 2
    }
}