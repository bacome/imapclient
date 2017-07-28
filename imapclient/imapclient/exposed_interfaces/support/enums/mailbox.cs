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

        messagecount = 1 << 16,
        recentcount = 1 << 17,
        uidnext = 1 << 18,
        uidnextunknowncount = 1 << 19,
        uidvalidity = 1 << 20,
        unseencount = 1 << 21,
        unseenunknowncount = 1 << 22,
        highestmodseq = 1 << 23,
        allstatus = messagecount | recentcount | uidnext | uidnextunknowncount | unseencount | unseenunknowncount | highestmodseq, // not uidvalidity because it is likely to have some heavy processing attached to it if it is monitored

        hasbeenselected = 1 << 24,
        hasbeenselectedforupdate = 1 << 25,
        hasbeenselectedreadonly = 1 << 26,
        messageflags = 1 << 27,
        forupdatepermanentflags = 1 << 28,
        readonlypermanentflags = 1 << 29,

        isselected = 1 << 30,
        isselectedforupdate = 1 << 31,
        isaccessreadonly = 1 << 32
    }

}