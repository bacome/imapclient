using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    internal partial class cBytesCursor
    {
        public bool GetRFC822LocalPart(out string rLocalPart)
        {
            var lBookmark = Position;

            List<string> lParts = new List<string>();
            string lPart;

            SkipRFC822CFWS();
            if (!GetRFC822Atom(out lPart) && !GetRFC822QuotedString(out lPart)) { Position = lBookmark; rLocalPart = null; return false; }
            lParts.Add(lPart);

            while (true)
            {
                SkipRFC822CFWS();
                lBookmark = Position;
                if (!SkipByte(cASCII.DOT)) break;
                SkipRFC822CFWS();
                if (!GetRFC822Atom(out lPart) && !GetRFC822QuotedString(out lPart)) { Position = lBookmark; break; }
                lParts.Add(lPart);
            }

            rLocalPart = string.Join(".", lParts);
            return true;
        }
    }
}