using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    internal static class cIMAPValidation
    {
        public static bool IsFetchableFlag(string pString)
        {
            if (pString == null) return false;
            if (pString.Length == 0) return false;

            string lFlag;
            if (pString[0] == '\\') lFlag = pString.Remove(0, 1);
            else lFlag = pString;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }
    }
}