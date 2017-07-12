using System;

namespace work.bacome.imapclient.support
{
    public interface iMailboxCacheItem
    {
        cListFlags ListFlags { get; } // not null
        cLSubFlags LSubFlags { get; } // if null use the values from the listflags
        cMailboxStatus MailboxStatus { get; } // not null
        long MailboxStatusAge { get; }
        cSelectedMailboxProperties SelectedMailboxProperties { get; } // not null
    }
}