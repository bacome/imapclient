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

        public List<cMailboxListItem> List(fListTypes pTypes = fListTypes.clientdefault, fListFlags pListFlags = fListFlags.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.List(NamespaceId, pTypes, pListFlags, pStatusAttributes);
        public Task<List<cMailboxListItem>> ListAsync(fListTypes pTypes = fListTypes.clientdefault, fListFlags pListFlags = fListFlags.clientdefault, fStatusAttributes pStatusAttributes = fStatusAttributes.clientdefault) => Client.ListAsync(NamespaceId, pTypes, pListFlags, pStatusAttributes);

        public override string ToString() => $"{nameof(cMailbox)}({NamespaceId})";
    }
}