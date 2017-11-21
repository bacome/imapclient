using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kStoreCommandPartLParenUnchangedSinceSpace = new cTextCommandPart("(UNCHANGEDSINCE ");
            private static readonly cCommandPart kStoreCommandPartRParenSpace = new cTextCommandPart(") ");
        }
    }
}