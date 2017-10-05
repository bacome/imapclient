using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kStoreCommandPartLParenUnchangedSinceSpace = new cCommandPart("(UNCHANGEDSINCE ");
            private static readonly cCommandPart kStoreCommandPartRParenSpace = new cCommandPart(") ");
        }
    }
}