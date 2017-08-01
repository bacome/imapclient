using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fMailboxProperties
    {
        exists = 1 << 0,

        canhavechildren = 1 << 1,
        canselect = 1 << 2,
        ismarked = 1 << 3,
        isnonexistent = 1 << 4,
        isremote = 1 << 5,
        haschildren = 1 << 6,
        containsall = 1 << 7,
        isarchive = 1 << 8,
        containsdrafts = 1 << 9,
        containsflagged = 1 << 10,
        containsjunk = 1 << 11,
        containssent = 1 << 12,
        containstrash = 1 << 13,

        issubscribed = 1 << 14,

        messagecount = 1 << 15,
        recentcount = 1 << 16,
        uidnext = 1 << 17,
        uidnextunknowncount = 1 << 18,
        uidvalidity = 1 << 19,
        unseencount = 1 << 20,
        unseenunknowncount = 1 << 21,
        highestmodseq = 1 << 22,
        allstatus = messagecount | recentcount | uidnext | uidnextunknowncount | unseencount | unseenunknowncount | highestmodseq, // not uidvalidity because it is likely to have some heavy processing attached to it if it is monitored

        hasbeenselected = 1 << 23,
        hasbeenselectedforupdate = 1 << 24,
        hasbeenselectedreadonly = 1 << 25,
        messageflags = 1 << 26,
        forupdatepermanentflags = 1 << 27,
        readonlypermanentflags = 1 << 28,

        isselected = 1 << 29,
        isselectedforupdate = 1 << 30,
        isaccessreadonly = 1 << 31
    }
}