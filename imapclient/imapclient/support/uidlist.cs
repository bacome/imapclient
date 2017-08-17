using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cUIDList : List<cUID>
        {
            public cUIDList() { }
            public cUIDList(cUID pUID) : base(new cUID[] { pUID }) { }
            public cUIDList(IList<cUID> pUIDs) : base(pUIDs) { }

            public override string ToString()
            {
                var lBuilder = new cListBuilder(nameof(cUIDList));
                foreach (var lUID in this) lBuilder.Append(lUID);
                return lBuilder.ToString();
            }
        }
    }
}