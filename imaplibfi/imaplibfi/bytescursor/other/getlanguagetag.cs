using System;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public partial class cBytesCursor
    {
        public bool GetLanguageTag(out string rLanguageTag)
        {
            // rfc 5646, 4646, 3066, 1766
            //  this is a crude implementation ignoring the finer details

            cByteList lResult = new cByteList();
            cByteList lPart;

            if (!GetToken(cCharset.Alpha, null, null, out lPart, 1, 8)) { rLanguageTag = null; return false; }

            lResult.AddRange(lPart);

            while (true)
            {
                var lBookmark = Position;
                if (!SkipByte(cASCII.HYPEN)) break;
                if (!GetToken(cCharset.AlphaNumeric, null, null, out lPart, 1, 8)) { Position = lBookmark; break; }
                lResult.Add(cASCII.HYPEN);
                lResult.AddRange(lPart);
            }

            rLanguageTag = cTools.ASCIIBytesToString(lResult);
            return true;
        }
    }
}