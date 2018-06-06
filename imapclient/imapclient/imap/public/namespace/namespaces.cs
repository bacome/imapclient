using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// An immutable collection of <see cref="cNamespace"/>.
    /// </summary>
    /// <seealso cref="cIMAPClient.Namespaces"/>
    public class cNamespaces
    {
        /**<summary>The 'personal' <see cref="cNamespace"/> instances in the collection. May be <see langword="null"/>.</summary>*/
        public readonly ReadOnlyCollection<cNamespace> Personal;
        /**<summary>The 'other user' <see cref="cNamespace"/> instances in the collection. May be <see langword="null"/>.</summary>*/
        public readonly ReadOnlyCollection<cNamespace> OtherUsers;
        /**<summary>The 'shared' <see cref="cNamespace"/> instances in the collection. May be <see langword="null"/>.</summary>*/
        public readonly ReadOnlyCollection<cNamespace> Shared;

        public readonly ReadOnlyCollection<cNamespaceName> All;

        internal cNamespaces(cIMAPClient pClient, IList<cNamespaceName> pPersonal, IList<cNamespaceName> pOtherUsers, IList<cNamespaceName> pShared)
        {
            if (pPersonal != null && pPersonal.Count == 0) throw new ArgumentOutOfRangeException(nameof(pPersonal));
            if (pOtherUsers != null && pOtherUsers.Count == 0) throw new ArgumentOutOfRangeException(nameof(pOtherUsers));
            if (pShared != null && pShared.Count == 0) throw new ArgumentOutOfRangeException(nameof(pShared));

            Personal = ZToNameSpaceCollection(pClient, pPersonal);
            OtherUsers = ZToNameSpaceCollection(pClient, pOtherUsers);
            Shared = ZToNameSpaceCollection(pClient, pShared);

            var lAll = new List<cNamespaceName>();

            if (pPersonal != null) lAll.AddRange(pPersonal);
            if (pOtherUsers != null) lAll.AddRange(pOtherUsers);
            if (pShared != null) lAll.AddRange(pShared);

            All = lAll.AsReadOnly();
        }

        private static ReadOnlyCollection<cNamespace> ZToNameSpaceCollection(cIMAPClient pClient, IList<cNamespaceName> pNames)
        {
            if (pNames == null) return null;
            List<cNamespace> lNamespaces = new List<cNamespace>();
            foreach (var lName in pNames) lNamespaces.Add(new cNamespace(pClient, lName));
            return lNamespaces.AsReadOnly();
        }

        /// <inheritdoc/>
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