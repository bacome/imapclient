using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public interface iMailboxes
    {
        List<cMailbox> Mailboxes(fMailboxProperties pProperties = fMailboxProperties.clientdefault);
        Task<List<cMailbox>> MailboxesAsync(fMailboxProperties pProperties = fMailboxProperties.clientdefault);
    }
}