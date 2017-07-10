using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMailboxProperties
    {
        clientdefault = 1 << 0,

        canhavechildren = 1 << 1,
        haschildren = 1 << 2,
        canselect = 1 << 3,
        ismarked = 1 << 4,
        issubscribed = 1 << 5,
        hassubscribedchildren = 1 << 6,
        islocal = 1 << 7,
        containsall = 1 << 8,
        isarchive = 1 << 9,
        containsdrafts = 1 << 10,
        containsflagged = 1 << 11,
        containsjunk = 1 << 12,
        containssent = 1 << 13,
        containstrash = 1 << 14,
        alllist = canhavechildren | haschildren | canselect | ismarked | islocal,
        alllsub = issubscribed | hassubscribedchildren | islocal,
        allspecialuse = containsall | isarchive | containsdrafts | containsflagged | containsjunk | containssent | containstrash,
        allflags = alllist | alllsub | allspecialuse,

        messagecount = 1 << 15,
        recentcount = 1 << 16,
        uidnext = 1 << 17,
        newunknownuidcount = 1 << 18,
        uidvalidity = 1 << 19,
        unseencount = 1 << 20,
        unseenunknowncount = 1 << 21,
        highestmodseq = 1 << 22,
        allstatus = messagecount | recentcount | uidnext | newunknownuidcount | uidvalidity | unseencount | unseenunknowncount | highestmodseq,

        // NOTE: this enum is part of a larger enum called fMailboxCacheProperties - move the values in that enum if adding values here
    }
}