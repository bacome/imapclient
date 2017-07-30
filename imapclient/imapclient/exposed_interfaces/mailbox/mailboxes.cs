using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public interface iMailboxes
    {
        List<cMailbox> Mailboxes(bool pStatus = false);
        Task<List<cMailbox>> MailboxesAsync(bool pStatus = false);
    }
}