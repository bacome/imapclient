using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxCacheDataSets
    {
        list = 1 << 0,
        lsub = 1 << 1,
        status = 1 << 2
    }
}