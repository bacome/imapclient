﻿using System;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        public bool GetURI(out cURIParts rParts, out string rString, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(GetURI));

            var lBookmark = Position;

            if (!cURIParts.Process(this, out rParts, lContext))
            {
                Position = lBookmark;
                rString = null;
                return false;
            }

            rString = GetFromAsString(lBookmark);
            return true;
        }
    }
}