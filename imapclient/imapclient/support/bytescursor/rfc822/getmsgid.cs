using System;

namespace work.bacome.imapclient
{
    internal partial class cBytesCursor
    {
        public bool GetRFC822MsgId(out string rMsgId)
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
                rMsgId = null;
                return false;
            }

            SkipRFC822CFWS();

            rMsgId = lLocalPart + "@" + lDomain;
            return true;
        }
    }
}