using System;

namespace work.bacome.imapclient.support
{
    public interface iMailboxCacheItem
    {
        bool? Exists { get; }
        cMailboxFlags MailboxFlags { get; } 
        cMailboxStatus MailboxStatus { get; }
        long MailboxStatusAge { get; }
        cMailboxSelected MailboxSelected { get; } // not null
    }
}