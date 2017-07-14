using System;

namespace work.bacome.imapclient.support
{
    public enum fMailboxProperties
    {
        exists = 1 << 0,

        mailboxflags = 1 << 1,
        canhavechildren = 1 << 2,
        canselect = 1 << 3,
        ismarked = 1 << 4,
        nonexistent = 1 << 5,
        issubscribed = 1 << 6,
        isremote = 1 << 7,
        haschildren = 1 << 8,
        hassubscribedchildren = 1 << 9,
        containsall = 1 << 10,
        isarchive = 1 << 11,
        containsdrafts = 1 << 12,
        containsflagged = 1 << 13,
        containsjunk = 1 << 14,
        containssent = 1 << 15,
        containstrash = 1 << 16,

        mailboxstatus = 1 << 17,
        messagecount = 1 << 18,
        recentcount = 1 << 19,
        uidnext = 1 << 20,
        newunknownuidcount = 1 << 21,
        uidvalidity = 1 << 22,
        unseencount = 1 << 23,
        unseenunknowncount = 1 << 24,
        highestmodseq = 1 << 25,

        mailboxbeenselected = 1 << 30,
        hasbeenselected = 1 << 31,
        hasbeenselectedforupdate = 1 << 32,
        hasbeenselectedreadonly = 1 << 33,
        messageflags = 1 << 34,
        forupdatepermanentflags = 1 << 35,
        readonlypermanentflags = 1 << 36

        mailboxselected = 1 << 26,
        isselected = 1 << 27,
        isselectedforupdate = 1 << 28,
        isaccessreadonly = 1 << 29,
    }
}