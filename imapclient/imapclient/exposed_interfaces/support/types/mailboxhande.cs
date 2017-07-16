using System;

namespace work.bacome.imapclient.support
{
    public interface iMailboxHandle
    {
        bool? Exists { get; }
        cMailboxFlags MailboxFlags { get; } 
        cMailboxStatus MailboxStatus { get; }
        long MailboxStatusAge { get; }
        cMailboxSelectedProperties MailboxSelectedProperties { get; } // not null
    }
}