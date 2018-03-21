﻿using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        internal bool GetRFC822Domain(out string rDomain)
        {
            if (GetRFC822DomainLiteral(out rDomain)) return true;

            List<string> lParts = new List<string>();
            string lPart;

            if (!GetRFC822Atom(out lPart)) { rDomain = null; return false; }
            lParts.Add(lPart);

            while (true)
            {
                var lBookmark = Position;
                if (!SkipByte(cASCII.DOT)) break;
                if (!GetRFC822Atom(out lPart)) { Position = lBookmark; break; }
                lParts.Add(lPart);
            }

            rDomain = string.Join(".", lParts);
            return true;
        }
    }
}