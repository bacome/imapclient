using System;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        public bool GetURL(out cURLParts rParts, out string rString, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(GetURL));

            var lBookmark = Position;

            if (!cURLParts.Process(this, out rParts, lContext))
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