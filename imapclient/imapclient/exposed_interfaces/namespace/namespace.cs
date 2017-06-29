using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cNamespace
    {
        public readonly cIMAPClient Client;
        public readonly cNamespaceId NamespaceId;

        public cNamespace(cIMAPClient pClient, cNamespaceId pNamespaceId)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            NamespaceId = pNamespaceId ?? throw new ArgumentNullException(nameof(pNamespaceId));
        }

        public string Prefix => NamespaceId.NamespaceName.Prefix;

        public List<cMailboxListItem> Mailboxes(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.Mailboxes(NamespaceId, pTypes, pFlagSets, pStatusAttributes);
        public Task<List<cMailboxListItem>> MailboxesAsync(fMailboxTypes pTypes = fMailboxTypes.clientdefault, fMailboxFlagSets pFlagSets = fMailboxFlagSets.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.MailboxesAsync(NamespaceId, pTypes, pFlagSets, pStatusAttributes);

        public override string ToString() => $"{nameof(cMailbox)}({NamespaceId})";
    }
}