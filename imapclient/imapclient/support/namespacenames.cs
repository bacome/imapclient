using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cNamespaceNames
        {
            public readonly ReadOnlyCollection<cNamespaceName> Personal;
            public readonly ReadOnlyCollection<cNamespaceName> OtherUsers;
            public readonly ReadOnlyCollection<cNamespaceName> Shared;

            public cNamespaceNames(List<cNamespaceName> pPersonal, List<cNamespaceName> pOtherUsers, List<cNamespaceName> pShared)
            {
                Personal = pPersonal?.AsReadOnly();
                OtherUsers = pOtherUsers?.AsReadOnly();
                Shared = pShared?.AsReadOnly();
            }
        }
    }
}