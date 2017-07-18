using System;

namespace work.bacome.imapclient.support
{
    public interface iMailboxHandle
    {
        object MailboxCache { get; }
        string EncodedMailboxName { get; }
        cMailboxName MailboxName { get; }
        bool? Exists { get; }
        cMailboxFlags MailboxFlags { get; } 
        cMailboxStatus MailboxStatus { get; }
        cMailboxSelectedProperties MailboxSelectedProperties { get; } // not null
    }
}