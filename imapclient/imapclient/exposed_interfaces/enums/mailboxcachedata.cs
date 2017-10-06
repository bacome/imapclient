using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxCacheData
    {
        subscribed = 1 << 0,
        children = 1 << 1,
        specialuse = 1 << 2,
        messagecount = 1 << 3,
        recentcount = 1 << 4,
        uidnext = 1 << 5,
        uidvalidity = 1 << 6,
        unseencount = 1 << 7,
        highestmodseq = 1 << 8,
        allstatus = messagecount | recentcount | uidnext | uidvalidity | unseencount | highestmodseq
    }
}