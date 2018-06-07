using System;
using System.Collections.Generic;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public sealed partial class cIMAPClient : cMailClient
    {
        private static cStrings ZGetNotPrefixedWith(cSession pSession, string pPrefix)
        {
            if (pSession == null) throw new ArgumentNullException(nameof(pSession));

            var lNamespaceNames = pSession.NamespaceNames;
            if (lNamespaceNames == null) return cStrings.Empty;

            var lNotPrefixedWith = new List<string>();

            ZGetNotPrefixedWithWorker(pPrefix, lNamespaceNames.Personal, lNotPrefixedWith);
            ZGetNotPrefixedWithWorker(pPrefix, lNamespaceNames.OtherUsers, lNotPrefixedWith);
            ZGetNotPrefixedWithWorker(pPrefix, lNamespaceNames.Shared, lNotPrefixedWith);

            return new cStrings(lNotPrefixedWith);
        }

        private static void ZGetNotPrefixedWithWorker(string pPrefix, IEnumerable<cNamespaceName> pNamespaceNames, List<string> pNotPrefixedWith)
        {
            if (pNamespaceNames == null) return;

            foreach (var lNamespaceName in pNamespaceNames)
                if (lNamespaceName.Prefix.Length > pPrefix.Length && lNamespaceName.Prefix.StartsWith(pPrefix))
                    pNotPrefixedWith.Add(lNamespaceName.Prefix);
        }
    }
}