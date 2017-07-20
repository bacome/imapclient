using System;

namespace work.bacome.imapclient
{
    public enum fMailboxProperties
    {
        clientdefault = 1 << 0,

        exists = 1 << 1,

        mailboxflags = 1 << 2,
        canhavechildren = 1 << 3,
        canselect = 1 << 4,
        ismarked = 1 << 5,
        nonexistent = 1 << 6,
        issubscribed = 1 << 7,
        isremote = 1 << 8,
        haschildren = 1 << 9,
        hassubscribedchildren = 1 << 10,
        containsall = 1 << 11,
        isarchive = 1 << 12,
        containsdrafts = 1 << 13,
        containsflagged = 1 << 14,
        containsjunk = 1 << 15,
        containssent = 1 << 16,
        containstrash = 1 << 17,

        status = 1 << 18,
        messagecount = 1 << 19,
        recentcount = 1 << 20,
        uidnext = 1 << 21,
        newunknownuidcount = 1 << 22,
        uidvalidity = 1 << 23,
        unseencount = 1 << 24,
        unseenunknowncount = 1 << 25,
        highestmodseq = 1 << 26,

        selectedproperties = 1 << 27,
        hasbeenselected = 1 << 28,
        hasbeenselectedforupdate = 1 << 29,
        hasbeenselectedreadonly = 1 << 30,
        messageflags = 1 << 31,
        forupdatepermanentflags = 1 << 32,
        readonlypermanentflags = 1 << 33,

        selected = 1 << 34,
        isselected = 1 << 35,
        isselectedforupdate = 1 << 36,
        isaccessreadonly = 1 << 37
    }
}