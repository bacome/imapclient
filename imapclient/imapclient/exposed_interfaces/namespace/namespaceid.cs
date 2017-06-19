using System;

namespace work.bacome.imapclient
{
    public class cNamespaceId
    {
        public readonly cAccountId AccountId;
        public readonly cNamespaceName NamespaceName;

        public cNamespaceId(cAccountId pAccountId, cNamespaceName pNamespaceName)
        {
            AccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
            NamespaceName = pNamespaceName ?? throw new ArgumentNullException(nameof(pNamespaceName));
        }
    }
}