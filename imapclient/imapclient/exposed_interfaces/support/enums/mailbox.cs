using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fListFlags
    {
        // rfc 3501
        noinferiors = 1 << 0, // hasnochildren will be set if this is set
        noselect = 1 << 1, // \ 
        marked = 1 << 2, //    > only one of these may be true
        unmarked = 1 << 3, // /

        // rfc 5258
        nonexistent = 1 << 4, // noselect will be set if this is set
        remote = 1 << 5,
        haschildren = 1 << 6, // rfc 3348
        hasnochildren = 1 << 7, // rfc 3348

        // next 7 rfc 6154 (specialuse)
        all = 1 << 8,
        archive = 1 << 9,
        drafts = 1 << 10,
        flagged = 1 << 11,
        junk = 1 << 12,
        sent = 1 << 13,
        trash = 1 << 14
    }

    [Flags]
    public enum fLSubFlags
    {
        subscribed = 1 << 0,
        hassubscribedchildren = 1 << 1
    }

    [Flags]
    public enum fMailboxProperties
    {
        exists = 1 << 0,

        canhavechildren = 1 << 1,
        canselect = 1 << 2,
        ismarked = 1 << 3,
        nonexistent = 1 << 4,
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
        hassubscribedchildren = 1 << 15,

        status = 1 << 16,
        messagecount = 1 << 17,
        recentcount = 1 << 18,
        uidnext = 1 << 19,
        newunknownuidcount = 1 << 20,
        uidvalidity = 1 << 21,
        unseencount = 1 << 22,
        unseenunknowncount = 1 << 23,
        highestmodseq = 1 << 24,

        selectedproperties = 1 << 25,
        hasbeenselected = 1 << 26,
        hasbeenselectedforupdate = 1 << 27,
        hasbeenselectedreadonly = 1 << 28,
        messageflags = 1 << 29,
        forupdatepermanentflags = 1 << 30,
        readonlypermanentflags = 1 << 31,

        selected = 1 << 32,
        isselected = 1 << 33,
        isselectedforupdate = 1 << 34,
        isaccessreadonly = 1 << 35
    }

}