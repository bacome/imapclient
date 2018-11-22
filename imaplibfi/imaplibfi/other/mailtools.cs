using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public static class cMailTools
    {
        public static string MessageId(string pIdLeft, string pIdRight)
        {
            if (pIdLeft == null) throw new ArgumentNullException(nameof(pIdLeft));
            if (pIdRight == null) throw new ArgumentNullException(nameof(pIdRight));
            if (cMailValidation.IsDotAtomText(pIdLeft)) return "<" + pIdLeft + "@" + pIdRight + ">";
            else return "<" + cTools.Enquote(pIdLeft) + "@" + pIdRight + ">";
        }

        public static string DateToRFC3501DateString(DateTime pDate) => string.Format("{0:dd}-{1}-{0:yyyy}", pDate, kRFCMonth.cName[pDate.Month]);

        public static bool TryEncodedMailboxPathToString(IList<byte> pEncodedMailboxPath, byte? pDelimiter, bool pUTF8Enabled, out string rString)
        {
            if (pUTF8Enabled) { rString = cTools.UTF8BytesToString(pEncodedMailboxPath); return true; }

            if (pDelimiter == null) return cModifiedUTF7.TryDecode(pEncodedMailboxPath, out rString, out _);

            byte lDelimiterByte = pDelimiter.Value;
            char lDelimiterChar = (char)lDelimiterByte;

            var lSegments = new List<cByteList>();

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

        public static bool IsValidDelimiter(char pDelimiter) => pDelimiter <= kChar.DEL && pDelimiter != kChar.NUL && pDelimiter != kChar.CR && pDelimiter != kChar.LF;
        public static bool IsValidDelimiter(byte pDelimiter) => pDelimiter <= cASCII.DEL && pDelimiter != cASCII.NUL && pDelimiter != cASCII.CR && pDelimiter != cASCII.LF;
    }
}