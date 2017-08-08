using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cNamespace : iChildMailboxes
    {
        public readonly cIMAPClient Client;
        public readonly cNamespaceName NamespaceName;

        public cNamespace(cIMAPClient pClient, cNamespaceName pNamespaceName)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            NamespaceName = pNamespaceName ?? throw new ArgumentNullException(nameof(pNamespaceName));
        }

        public string Prefix => NamespaceName.Prefix;

        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(NamespaceName, pDataSets);
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(NamespaceName, pDataSets);

        public List<cMailbox> Subscribed(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(NamespaceName, pDescend, pDataSets);
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(NamespaceName, pDescend, pDataSets);

        public override string ToString() => $"{nameof(cMailbox)}({NamespaceName})";
    }
}