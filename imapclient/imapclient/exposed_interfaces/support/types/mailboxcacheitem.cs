using System;

namespace work.bacome.imapclient.support
{
    public interface iMailboxCacheItem
    {
        cMailboxFlags MailboxFlags { get; }
        bool IsSelected { get; }
        bool IsSelectedForUpdate { get; }
        bool IsAccessReadOnly { get; }
        bool HasBeenSelected { get; }
        bool HasBeenSelectedForUpdate { get; }
        bool HasBeenSelectedReadOnly { get; }
        cMessageFlags MessageFlags { get; }
        cMessageFlags ForUpdatePermanentFlags { get; }
        cMessageFlags ReadOnlyPermanentFlags { get; }
        cMailboxStatus MailboxStatus { get; }
        long MailboxStatusAge { get; }
    }
}