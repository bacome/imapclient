using System;

namespace work.bacome.imapclient
{
    [Flags]
    internal enum fMailboxProperties
    {
        exists = 1 << 0,

        canhavechildren = 1 << 1,
        canselect = 1 << 2,
        ismarked = 1 << 3,
        isremote = 1 << 4,
        haschildren = 1 << 5,
        containsall = 1 << 6,
        isarchive = 1 << 7,
        containsdrafts = 1 << 8,
        containsflagged = 1 << 9,
        containsjunk = 1 << 10,
        containssent = 1 << 11,
        containstrash = 1 << 12,

        issubscribed = 1 << 13,

        messagecount = 1 << 14,
        recentcount = 1 << 15,
        uidnext = 1 << 16,
        uidnextunknowncount = 1 << 17,
        uidvalidity = 1 << 18,
        unseencount = 1 << 19,
        unseenunknowncount = 1 << 20,
        highestmodseq = 1 << 21,

        hasbeenselected = 1 << 22,
        hasbeenselectedforupdate = 1 << 23,
        hasbeenselectedreadonly = 1 << 24,
        uidnotsticky = 1 << 25,
        messageflags = 1 << 26,
        forupdatepermanentflags = 1 << 27,
        readonlypermanentflags = 1 << 28,

        isselected = 1 << 29,
        isselectedforupdate = 1 << 30,
        isaccessreadonly = 1 << 31
    }
}