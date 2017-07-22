using System;

namespace work.bacome.imapclient
{
    public enum fMailboxProperties
    {
        clientdefault = 1 << 0,

        exists = 1 << 1,

        listflags = 1 << 2,
        canhavechildren = 1 << 3,
        canselect = 1 << 4,
        ismarked = 1 << 5,
        nonexistent = 1 << 6,
        isremote = 1 << 7,
        haschildren = 1 << 8,
        containsall = 1 << 9,
        isarchive = 1 << 10,
        containsdrafts = 1 << 11,
        containsflagged = 1 << 12,
        containsjunk = 1 << 13,
        containssent = 1 << 14,
        containstrash = 1 << 15,

        lsubflags = 1 << 16,
        issubscribed = 1 << 17,
        hassubscribedchildren = 1 << 18,

        status = 1 << 19,
        messagecount = 1 << 20,
        recentcount = 1 << 21,
        uidnext = 1 << 22,
        newunknownuidcount = 1 << 23,
        uidvalidity = 1 << 24,
        unseencount = 1 << 25,
        unseenunknowncount = 1 << 26,
        highestmodseq = 1 << 27,

        selectedproperties = 1 << 28,
        hasbeenselected = 1 << 29,
        hasbeenselectedforupdate = 1 << 30,
        hasbeenselectedreadonly = 1 << 31,
        messageflags = 1 << 32,
        forupdatepermanentflags = 1 << 33,
        readonlypermanentflags = 1 << 34,

        selected = 1 << 35,
        isselected = 1 << 36,
        isselectedforupdate = 1 << 37,
        isaccessreadonly = 1 << 38
    }
}