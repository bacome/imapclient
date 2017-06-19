using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cMailboxNamePatterns : List<cMailboxNamePattern>
            {
                public cMailboxNamePatterns() { }

                public bool Matches(string pMailboxName)
                {
                    foreach (var lPattern in this) if (lPattern.Matches(pMailboxName)) return true;
                    return false;
                }

                public override string ToString()
                {
                    var lBuilder = new cListBuilder(nameof(cMailboxNamePatterns));
                    foreach (var lPattern in this) lBuilder.Append(lPattern);
                    return lBuilder.ToString();
                }
            }
        }
    }
}