using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    internal partial class cBytesCursor
    {
        internal bool GetRFC822Domain(out string rDomain)
        {
            if (GetDomainLiteral(out rDomain)) return true;

            var lBookmark = Position;

            List<string> lParts = new List<string>();
            string lPart;

            SkipRFC822CFWS();
            if (!GetRFC822Atom(out lPart)) { Position = lBookmark; rDomain = null; return false; }
            lParts.Add(lPart);

            while (true)
            {
                SkipRFC822CFWS();
                lBookmark = Position;
                if (!SkipByte(cASCII.DOT)) break;
                SkipRFC822CFWS();
                if (!GetRFC822Atom(out lPart)) { Position = lBookmark; break; }
                lParts.Add(lPart);
            }

            rDomain = string.Join(".", lParts);
            return true;
        }
    }
}