using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    public partial class cBytesCursor
    {
        public bool GetRFC822LocalPart(out string rLocalPart)
        {
            var lParts = new List<string>();
            string lPart;

            if (!GetRFC822Atom(out lPart) && !GetRFC822QuotedString(out lPart)) { rLocalPart = null; return false; }
            lParts.Add(lPart);

            while (true)
            {
                var lBookmark = Position;
                if (!SkipByte(cASCII.DOT)) break;
                if (!GetRFC822Atom(out lPart) && !GetRFC822QuotedString(out lPart)) { Position = lBookmark; break; }
                lParts.Add(lPart);
            }

            rLocalPart = string.Join(".", lParts);
            return true;
        }
    }
}