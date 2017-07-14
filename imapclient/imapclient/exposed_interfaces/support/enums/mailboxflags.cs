using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fMailboxFlags
    {
        // rfc 3501
        noinferiors = 1 << 0, // hasnochildren will be set if this is set
        noselect = 1 << 1, // \ 
        marked = 1 << 2, //    > only one of these may be true
        unmarked = 1 << 3, // /

        // rfc 5258
        nonexistent = 1 << 4, // noselect will be set if this is set
        subscribed = 1 << 5,
        remote = 1 << 6,
        haschildren = 1 << 7, // rfc 3348
        hasnochildren = 1 << 8, // rfc 3348

        hassubscribedchildren = 1 << 9, // derived from the CHILDINFO response

        // next 7 rfc 6154 (specialuse)
        all = 1 << 10,
        archive = 1 << 11,
        drafts = 1 << 12,
        flagged = 1 << 13,
        junk = 1 << 14,
        sent = 1 << 15,
        trash = 1 << 16
    }
}