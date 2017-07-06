using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fMailboxCacheItemDifferences
    {
        selected = 1,
        selectedforupdate = 1 << 1,
        accessreadonly = 1 << 2,
        flags = 1 << 3,
        permanentflags = 1 << 4,
        status = 1 << 5,
        messagecount = 1 << 6,
        recent = 1 << 7,
        uidnext = 1 << 8,
        newunknownuid = 1 << 9,
        uidvalidity = 1 << 10,
        unseen = 1 << 11,
        unseenunknown = 1 << 12,
        highestmodseq = 1 << 13,
        all = 0b11111111111111
    }


    public interface iMailboxCacheItem
    {
        bool Selected { get; }
        bool SelectedForUpdate { get; }
        bool AccessReadOnly { get; }
        cMessageFlags Flags { get; }
        cMessageFlags PermanentFlags { get; }
        cMailboxStatus Status { get; }
        long StatusAge { get; }
    }
}