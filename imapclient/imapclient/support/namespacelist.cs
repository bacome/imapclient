using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cNamespaceList : List<cNamespaceName>
        {
            public cNamespaceList() { }

            // special constructor for use in connect
            public cNamespaceList(char? pDelimiter) { Add(new cNamespaceName(string.Empty, pDelimiter)); }

            public override string ToString()
            {
                cListBuilder lBuilder = new cListBuilder(nameof(cNamespaceList));
                foreach (var lNamespace in this) lBuilder.Append(lNamespace);
                return lBuilder.ToString();
            }
        }
    }
}