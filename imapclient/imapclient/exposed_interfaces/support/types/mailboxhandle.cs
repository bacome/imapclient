using System;

namespace work.bacome.imapclient.support
{
    public interface iMailboxHandle
    {
        object Cache { get; }
        string EncodedMailboxName { get; }
        cMailboxName MailboxName { get; }
        cCommandPart MailboxNameCommandPart { get; }
        bool? Exists { get; }
        cListFlags ListFlags { get; }
        cLSubFlags LSubFlags { get; } 
        cStatus Status { get; }
        cMailboxStatus MailboxStatus { get; }
        cMailboxSelectedProperties SelectedProperties { get; } // not null
    }
}