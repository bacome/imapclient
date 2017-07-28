using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cNamespace : iMailboxes
    {
        public readonly cIMAPClient Client;
        public readonly cNamespaceName NamespaceName;

        public cNamespace(cIMAPClient pClient, cNamespaceName pNamespaceName)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            NamespaceName = pNamespaceName ?? throw new ArgumentNullException(nameof(pNamespaceName));
        }

        public string Prefix => NamespaceName.Prefix;

        public List<cMailbox> Mailboxes(bool pStatus = false) => Client.Mailboxes(NamespaceName, pStatus);
        public Task<List<cMailbox>> MailboxesAsync(bool pStatus = false) => Client.MailboxesAsync(NamespaceName, pStatus);

        public List<cMailbox> SubscribedMailboxes(bool pStatus = false) => Client.SubscribedMailboxes(NamespaceName, pStatus);
        public Task<List<cMailbox>> SubscribedMailboxesAsync(bool pStatus = false) => Client.SubscribedMailboxesAsync(NamespaceName, pStatus);

        public override string ToString() => $"{nameof(cMailbox)}({NamespaceName})";
    }
}