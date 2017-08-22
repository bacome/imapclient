using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cNamespace : iMailboxParent
    {
        public readonly cIMAPClient Client;
        public readonly cNamespaceName NamespaceName;

        public cNamespace(cIMAPClient pClient, cNamespaceName pNamespaceName)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            NamespaceName = pNamespaceName ?? throw new ArgumentNullException(nameof(pNamespaceName));
        }

        public string Prefix => NamespaceName.Prefix;
        public char? Delimiter => NamespaceName.Delimiter;

        public List<cMailbox> Mailboxes(fMailboxCacheDataSets pDataSets = 0) => Client.Mailboxes(NamespaceName, pDataSets);
        public Task<List<cMailbox>> MailboxesAsync(fMailboxCacheDataSets pDataSets = 0) => Client.MailboxesAsync(NamespaceName, pDataSets);

        public List<cMailbox> Subscribed(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.Subscribed(NamespaceName, pDescend, pDataSets);
        public Task<List<cMailbox>> SubscribedAsync(bool pDescend = true, fMailboxCacheDataSets pDataSets = 0) => Client.SubscribedAsync(NamespaceName, pDescend, pDataSets);

        public cMailbox CreateChild(string pMailboxName, bool pAsFutureParent = true) => Client.Create(ZCreateChild(pMailboxName), pAsFutureParent);
        public Task<cMailbox> CreateChildAsync(string pMailboxName, bool pAsFutureParent = true) => Client.CreateAsync(ZCreateChild(pMailboxName), pAsFutureParent);

        private cMailboxName ZCreateChild(string pMailboxName)
        {
            if (NamespaceName.Delimiter == null) throw new InvalidOperationException();
            if (string.IsNullOrEmpty(pMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
            if (pMailboxName.IndexOf(NamespaceName.Delimiter.Value) != -1) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
            if (!cMailboxName.TryConstruct(NamespaceName.Prefix + pMailboxName, NamespaceName.Delimiter, out var lMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));
            return lMailboxName;
        }

        public override string ToString() => $"{nameof(cMailbox)}({NamespaceName})";
    }
}