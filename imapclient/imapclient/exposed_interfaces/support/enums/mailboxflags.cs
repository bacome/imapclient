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
}