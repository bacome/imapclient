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

        public List<cMailboxListItem> Mailboxes(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.Mailboxes(NamespaceId, pTypes, pFlagSets, pStatusAttributes);
        public Task<List<cMailboxListItem>> MailboxesAsync(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.MailboxesAsync(NamespaceId, pTypes, pFlagSets, pStatusAttributes);

        public override string ToString() => $"{nameof(cMailbox)}({NamespaceName})";
    }
}