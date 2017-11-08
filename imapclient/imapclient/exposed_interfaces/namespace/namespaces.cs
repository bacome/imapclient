using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    public class cNamespaces
    {
        public readonly ReadOnlyCollection<cNamespace> Personal;
        public readonly ReadOnlyCollection<cNamespace> OtherUsers;
        public readonly ReadOnlyCollection<cNamespace> Shared;

        public cNamespaces(cIMAPClient pClient, IList<cNamespaceName> pPersonal, IList<cNamespaceName> pOtherUsers, IList<cNamespaceName> pShared)
        {
            if (pPersonal != null && pPersonal.Count == 0) throw new ArgumentOutOfRangeException(nameof(pPersonal));
            if (pOtherUsers != null && pOtherUsers.Count == 0) throw new ArgumentOutOfRangeException(nameof(pOtherUsers));
            if (pShared != null && pShared.Count == 0) throw new ArgumentOutOfRangeException(nameof(pShared));

            Personal = ZToNameSpaceCollection(pClient, pPersonal);
            OtherUsers = ZToNameSpaceCollection(pClient, pOtherUsers);
            Shared = ZToNameSpaceCollection(pClient, pShared);
        }

        private static ReadOnlyCollection<cNamespace> ZToNameSpaceCollection(cIMAPClient pClient, IList<cNamespaceName> pNames)
        {
            if (pNames == null) return null;
            List<cNamespace> lNamespaces = new List<cNamespace>();
            foreach (var lName in pNames) lNamespaces.Add(new cNamespace(pClient, lName));
            return lNamespaces.AsReadOnly();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cNamespaces));
            lBuilder.Append(ZToStringWorker(nameof(Personal), Personal));
            lBuilder.Append(ZToStringWorker(nameof(OtherUsers), OtherUsers));
            lBuilder.Append(ZToStringWorker(nameof(Shared), Shared));
            return lBuilder.ToString();
        }

        private static string ZToStringWorker(string pListName, IList<cNamespace> pNamespaces)
        {
            cListBuilder lBuilder = new cListBuilder(pListName);
            if (pNamespaces != null) foreach (cNamespace lNamespace in pNamespaces) lBuilder.Append(lNamespace);
            return lBuilder.ToString();
        }
    }
}