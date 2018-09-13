using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kFetchCommandPartFetchSpace = new cTextCommandPart("FETCH ");
            private static readonly cCommandPart kFetchCommandPartSpaceLParenUID = new cTextCommandPart(" (UID");
            private static readonly cCommandPart kFetchCommandPartUIDFetchSpace = new cTextCommandPart("UID FETCH ");
            private static readonly cCommandPart kFetchCommandPartSpaceBodyPeekLBracket = new cTextCommandPart(" BODY.PEEK[");
            private static readonly cCommandPart kFetchCommandPartSpaceBinaryPeekLBracket = new cTextCommandPart(" BINARY.PEEK[");
            private static readonly cCommandPart kFetchCommandPartSpaceBinarySizeLBracket = new cTextCommandPart(" BINARY.SIZE[");
        }
    }
}