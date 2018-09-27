using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    internal static class cValidation
    {
        public static bool IsSectionPart(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            var lCursor = new cBytesCursor(pString);

            while (true)
            {
                if (!lCursor.GetNZNumber(out _, out _)) return false;
                if (!lCursor.SkipByte(cASCII.DOT)) break;
            }

            return lCursor.Position.AtEnd;
        }

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