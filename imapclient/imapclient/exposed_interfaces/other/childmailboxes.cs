using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public interface iChildMailboxes
    {
        List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0);
        Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0);
        List<cMailbox> Subscribed(fMailboxCacheDataSets pDataSets = 0);
        Task<List<cMailbox>> SubscribedAsync(fMailboxCacheDataSets pDataSets = 0);
    }
}