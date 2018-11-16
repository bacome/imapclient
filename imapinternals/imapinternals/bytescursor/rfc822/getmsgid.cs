using System;

namespace work.bacome.imapclient
{
    public partial class cBytesCursor
    {
        public bool GetRFC822MsgId(out string rIdLeft, out string rIdRight)
        {
            var lBookmark = Position;

            SkipRFC822CFWS();

            if (!SkipByte(cASCII.LESSTHAN) ||
                !GetRFC822LocalPart(out rIdLeft) ||
                !SkipByte(cASCII.AT) ||
                !GetRFC822Domain(out rIdRight) ||
                !SkipByte(cASCII.GREATERTHAN)
               )
            {
                Position = lBookmark;
                rIdLeft = null;
                rIdRight = null;
                return false;
            }

            SkipRFC822CFWS();

            return true;
        }
    }
}