using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public interface iMailboxParent
    {
        char? Delimiter { get; }
        List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0);
        Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0);
        List<cMailbox> Subscribed(bool pDescend, fMailboxCacheDataSets pDataSets = 0);
        Task<List<cMailbox>> SubscribedAsync(bool pDescend, fMailboxCacheDataSets pDataSets = 0);
        cMailbox CreateChild(string pName, bool pAsFutureParent);
        Task<cMailbox> CreateChildAsync(string pName, bool pAsFutureParent);
    }
}