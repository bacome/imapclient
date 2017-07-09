using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxProperties
    {
        canhavechildren = 1,
        haschildren = 1 << 1,
        canselect = 1 << 2,
        ismarked = 1 << 3,
        issubscribed = 1 << 4,
        hassubscribedchildren = 1 << 5,
        islocal = 1 << 6,
        containsall = 1 << 7,
        isarchive = 1 << 8,
        containsdrafts = 1 << 9,
        containsflagged = 1 << 10,
        containsjunk = 1 << 11,
        containssent = 1 << 12,
        containstrash = 1 << 13,

        messagecount = 1 << 14,
        recentcount = 1 << 15,
        uidnext = 1 << 16,
        newunknownuidcount = 1 << 17,
        uidvalidity = 1 << 18,
        unseencount = 1 << 19,
        unseenunknowncount = 1 << 20,
        highestmodseq = 1 << 21

        // NOTE: this enum is part of a larger enum called fMailboxCacheProperties - move the values in that enum if adding values here
    }
}