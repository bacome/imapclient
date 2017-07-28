using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public interface iMailboxes
    {
        List<cMailbox> Mailboxes(bool pStatus);
        Task<List<cMailbox>> MailboxesAsync(bool pStatus);
        List<cMailbox> SubscribedMailboxes(bool pStatus);
        Task<List<cMailbox>> SubscribedMailboxesAsync(bool pStatus);
    }
}