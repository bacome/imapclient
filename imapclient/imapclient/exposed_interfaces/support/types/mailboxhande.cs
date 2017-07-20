using System;

namespace work.bacome.imapclient.support
{
    public interface iMailboxHandle
    {
        object Cache { get; }
        string EncodedMailboxName { get; }
        cCommandPart CommandPart { get; }
        cMailboxName MailboxName { get; }
        bool? Exists { get; }
        cMailboxFlags MailboxFlags { get; } 
        cMailboxStatus MailboxStatus { get; }
        cMailboxSelectedProperties SelectedProperties { get; } // not null
    }
}