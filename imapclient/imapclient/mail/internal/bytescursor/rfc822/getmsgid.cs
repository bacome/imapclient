using System;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        public bool GetRFC822MsgId(out string rMessageId)
        {
            var lBookmark = Position;

            SkipRFC822CFWS();

            if (!SkipByte(cASCII.LESSTHAN) ||
                !GetRFC822LocalPart(out string lLocalPart) ||
                !SkipByte(cASCII.AT) ||
                !GetRFC822Domain(out string lDomain) ||
                !SkipByte(cASCII.GREATERTHAN)
               )
            {
                Position = lBookmark;
                rMessageId = null;
                return false;
            }

            SkipRFC822CFWS();

            rMessageId = lLocalPart + "@" + lDomain;
            return true;
        }
    }
}