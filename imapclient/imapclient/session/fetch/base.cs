using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kFetchCommandPartFetchSpace = new cCommandPart("FETCH ");
            private static readonly cCommandPart kFetchCommandPartUIDFetchSpace = new cCommandPart("UID FETCH ");
            private static readonly cCommandPart kFetchCommandPartSpaceBodyPeekLBracket = new cCommandPart(" BODY.PEEK[");
            private static readonly cCommandPart kFetchCommandPartSpaceBinaryPeekLBracket = new cCommandPart(" BINARY.PEEK[");
            private static readonly cCommandPart kFetchCommandPartSpaceBinarySizeLBracket = new cCommandPart(" BINARY.SIZE[");
        }
    }
}