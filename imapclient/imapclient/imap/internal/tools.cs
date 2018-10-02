using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    internal static class cIMAPTools
    {
        public static string DateToRFC3501DateString(DateTime pDate) => string.Format("{0:dd}-{1}-{0:yyyy}", pDate, cRFCMonth.cName[pDate.Month]);

        public static bool TryEncodedMailboxPathToString(IList<byte> pEncodedMailboxPath, byte? pDelimiter, bool pUTF8Enabled, out string rString)
        {
            if (pUTF8Enabled) { rString = cMailTools.UTF8BytesToString(pEncodedMailboxPath); return true; }

            if (pDelimiter == null) return cModifiedUTF7.TryDecode(pEncodedMailboxPath, out rString, out _);

            byte lDelimiterByte = pDelimiter.Value;
            char lDelimiterChar = (char)lDelimiterByte;

            List<cByteList> lSegments = new List<cByteList>();

            cByteList lSegment = new cByteList();

            foreach (byte lByte in pEncodedMailboxPath)
            {
                if (lByte == lDelimiterByte)
                {
                    lSegments.Add(lSegment);
                    lSegment = new cByteList();
                }
                else lSegment.Add(lByte);
            }

            lSegments.Add(lSegment);

            StringBuilder lResult = new StringBuilder();
            bool lFirst = true;

            foreach (var lSegmentBytes in lSegments)
            {
                if (lFirst) lFirst = false;
                else lResult.Append(lDelimiterChar);

                if (!cModifiedUTF7.TryDecode(lSegmentBytes, out var lSegmentString, out _)) { rString = null; return false; }
                lResult.Append(lSegmentString);
            }

            rString = lResult.ToString();
            return true;
        }

        public static bool IsValidDelimiter(char pDelimiter) => pDelimiter <= cChar.DEL && pDelimiter != cChar.NUL && pDelimiter != cChar.CR && pDelimiter != cChar.LF;
        public static bool IsValidDelimiter(byte pDelimiter) => pDelimiter <= cASCII.DEL && pDelimiter != cASCII.NUL && pDelimiter != cASCII.CR && pDelimiter != cASCII.LF;
    }
}
