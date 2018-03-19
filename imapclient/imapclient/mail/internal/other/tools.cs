using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal static class cTools
    {
        private static readonly IdnMapping kIDNMapping = new IdnMapping();

        public static string ASCIIBytesToString(IList<byte> pBytes)
        {
            if (pBytes == null || pBytes.Count == 0) return string.Empty;
            char[] lChars = new char[pBytes.Count];
            for (int i = 0; i < pBytes.Count; i++) lChars[i] = (char)pBytes[i];
            return new string(lChars);
        }

        public static string ASCIIBytesToString(char pPrefix, IList<byte> pBytes)
        {
            if (pBytes == null) return new string(pPrefix, 1);
            char[] lChars = new char[pBytes.Count + 1];
            lChars[0] = pPrefix;
            for (int i = 0, j = 1; i < pBytes.Count; i++, j++) lChars[j] = (char)pBytes[i];
            return new string(lChars);
        }

        public static string UTF8BytesToString(IList<byte> pBytes)
        {
            if (pBytes == null || pBytes.Count == 0) return string.Empty;
            byte[] lBytes = new byte[pBytes.Count];
            for (int i = 0; i < pBytes.Count; i++) lBytes[i] = pBytes[i];
            return new string(Encoding.UTF8.GetChars(lBytes));
        }

        public static string BytesToLoggableString(IList<byte> pBytes)
        {
            if (pBytes == null) return string.Empty;

            StringBuilder lBuilder = new StringBuilder();

            foreach (byte lByte in pBytes)
            {
                if (lByte < cASCII.SPACE || lByte > cASCII.TILDA || lByte == cASCII.GRAVE) lBuilder.AppendFormat("`{0,2:X2}", lByte);
                else lBuilder.Append((char)lByte);
            }

            return lBuilder.ToString();
        }

        public static string BytesToLoggableString(string pNameOfClass, IList<byte> pBytes, int pMaxLength)
        {
            if (pBytes == null) return pNameOfClass + "()";

            StringBuilder lBuilder = new StringBuilder($"{pNameOfClass}(");

            foreach (byte lByte in pBytes)
            {
                if (pMaxLength-- == 0)
                {
                    lBuilder.Append("```");
                    break;
                }

                if (lByte < cASCII.SPACE || lByte > cASCII.TILDA || lByte == cASCII.GRAVE) lBuilder.AppendFormat("`{0,2:X2}", lByte);
                else lBuilder.Append((char)lByte);
            }

            return lBuilder.ToString() + ")";
        }

        public static bool TryEncodedMailboxPathToString(IList<byte> pEncodedMailboxPath, byte? pDelimiter, bool pUTF8Enabled, out string rString)
        {
            if (pUTF8Enabled) { rString = UTF8BytesToString(pEncodedMailboxPath); return true; }

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

        public static cByteList UIntToBytesReverse(uint pNumber)
        {
            cByteList lBytes = new cByteList(10);

            uint lNumber = pNumber;

            do
            {
                int lDigit = (int)(lNumber % 10);
                lBytes.Add((byte)(cASCII.ZERO + lDigit));
                lNumber = lNumber / 10;
            } while (lNumber > 0);

            return lBytes;
        }

        public static cByteList ULongToBytesReverse(ulong pNumber)
        {
            cByteList lBytes = new cByteList(20);

            ulong lNumber = pNumber;

            do
            {
                int lDigit = (int)(lNumber % 10);
                lBytes.Add((byte)(cASCII.ZERO + lDigit));
                lNumber = lNumber / 10;
            } while (lNumber > 0);

            return lBytes;
        }

        public static cByteList IntToBytesReverse(int pNumber)
        {
            if (pNumber < 0) throw new ArgumentOutOfRangeException(nameof(pNumber));

            cByteList lBytes = new cByteList(10);

            int lNumber = pNumber;

            do
            {
                int lDigit = lNumber % 10;
                lBytes.Add((byte)(cASCII.ZERO + lDigit));
                lNumber = lNumber / 10;
            } while (lNumber > 0);

            return lBytes;
        }

        public static bool IsValidDelimiter(char pDelimiter) => pDelimiter <= cChar.DEL && pDelimiter != cChar.NUL && pDelimiter != cChar.CR && pDelimiter != cChar.LF;
        public static bool IsValidDelimiter(byte pDelimiter) => pDelimiter <= cASCII.DEL && pDelimiter != cASCII.NUL && pDelimiter != cASCII.CR && pDelimiter != cASCII.LF;

        public static bool TryCharsetBytesToString(string pCharset, IList<byte> pBytes, out string rString)
        {
            // the null handling here is for RFC 2231 format where the charset and the value are optional

            if (pBytes == null) { rString = null; return true; }
            if (pCharset == null) { rString = UTF8BytesToString(pBytes); return true; }

            byte[] lBytes = new byte[pBytes.Count];
            pBytes.CopyTo(lBytes, 0);

            try
            {
                rString = new string(Encoding.GetEncoding(pCharset).GetChars(lBytes));
                return true;
            }
            catch
            {
                rString = null;
                return false;
            }
        }

        public static Exception Flatten(AggregateException pException)
        {
            var lException = pException.Flatten();
            if (lException.InnerExceptions.Count == 1) return lException.InnerExceptions[0];
            return pException;
        }

        public static List<byte> GetCharsetNameBytes(Encoding pEncoding)
        {
            if (pEncoding == null) throw new ArgumentNullException(nameof(pEncoding));
            List<byte> lResult = new List<byte>();
            foreach (char lChar in pEncoding.WebName) lResult.Add((byte)lChar);
            return lResult;
        }

        public static byte[] ByteToHexBytes(byte pByte)
        {
            byte[] lResult = new byte[2];
            lResult[0] = LHexDigit(pByte >> 4);
            lResult[1] = LHexDigit(pByte & 0b1111);
            return lResult;

            byte LHexDigit(int pNibble)
            {
                if (pNibble < 10) return (byte)(cASCII.ZERO + pNibble);
                return (byte)(cASCII.A + pNibble - 10);
            }
        }

        public static string GetRFC822DateTimeString(DateTime pDateTime)
        {
            string lSign;
            string lZone;

            if (pDateTime.Kind == DateTimeKind.Local)
            {
                var lOffset = TimeZoneInfo.Local.GetUtcOffset(pDateTime);

                if (lOffset < TimeSpan.Zero)
                {
                    lSign = "-";
                    lOffset = -lOffset;
                }
                else lSign = "+";

                lZone = lOffset.ToString("hhmm");
            }
            else if (pDateTime.Kind == DateTimeKind.Utc)
            {
                lSign = "+";
                lZone = "0000";
            }
            else
            {
                lSign = "-";
                lZone = "0000";
            }

            var lMonth = cRFCMonth.cName[pDateTime.Month - 1];

            return string.Format("{0:dd} {1} {0:yyyy} {0:HH}:{0:mm}:{0:ss} {2}{3}", pDateTime, lMonth, lSign, lZone);
        }

        public static string GetRFC822DateTimeString(DateTimeOffset pDateTimeOffset)
        {
            string lSign;
            string lZone;

            var lOffset = pDateTimeOffset.Offset;

            if (lOffset < TimeSpan.Zero)
            {
                lSign = "-";
                lOffset = -lOffset;
            }
            else lSign = "+";

            lZone = lOffset.ToString("hhmm");

            var lMonth = cRFCMonth.cName[pDateTimeOffset.Month - 1];

            return string.Format("{0:dd} {1} {0:yyyy} {0:HH}:{0:mm}:{0:ss} {2}{3}", pDateTimeOffset, lMonth, lSign, lZone);
        }

        public static string GetDNSHost(string pHost) => kIDNMapping.GetAscii(pHost);

        public static string GetDisplayHost(string pHost)
        {
            // if the domain is already unicode, this will normalise it
            //  (note that calling IdnMapping.GetUnicode on a string with unicode in it will throw)
            if (pHost == null) return null;
            var lASCII = kIDNMapping.GetAscii(pHost);
            return kIDNMapping.GetUnicode(lASCII);
        }

        public static bool IsDotAtom(string pText)
        {
            if (pText == null) return false;
            var lAtoms = pText.Split('.');
            foreach (var lAtom in lAtoms) if (lAtom.Length == 0 || !cCharset.AText.ContainsAll(lAtom)) return false;
            return true;
        }

        public static bool IsDomainLiteral(string pText, out string rDText)
        {
            if (pText == null) { rDText = null; return false; }
            cBytesCursor lCursor = new cBytesCursor(pText);
            lCursor.SkipRFC822CFWS();
            if (!lCursor.SkipByte(cASCII.LBRACKET)) { rDText = null; return false; }
            lCursor.SkipRFC822FWS();
            if (!lCursor.GetToken(cCharset.DText, null, null, out rDText)) return false;
            lCursor.SkipRFC822FWS();
            if (!lCursor.SkipByte(cASCII.RBRACKET)) return false;
            lCursor.SkipRFC822CFWS();
            return lCursor.Position.AtEnd;
        }

        public static bool IsDomain(string pText) => IsDomainLiteral(pText, out _) || IsDotAtom(pText);

        public static List<cEmailAddress> MailAddressesToEmailAddresses(IEnumerable<MailAddress> pMailAddresses)
        {
            if (pMailAddresses == null) throw new ArgumentNullException(nameof(pMailAddresses));

            var lEmailAddresses = new List<cEmailAddress>();

            foreach (var lMailAddress in pMailAddresses)
            {
                if (lMailAddress == null) throw new ArgumentOutOfRangeException(nameof(pMailAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lEmailAddresses.Add(lMailAddress);
            }

            return lEmailAddresses;
        }

        public static bool ContainsNonASCII(IEnumerable<char> pChars)
        {
            foreach (var lChar in pChars) if (lChar > cChar.DEL) return true;
            return false;
        }

        public static string Enquote(IEnumerable<char> pChars)
        {
            if (pChars == null) return null;

            var lBuilder = new StringBuilder();

            lBuilder.Append('"');

            foreach (char lChar in pChars)
            {
                if (lChar == '"' || lChar == '\\') lBuilder.Append('\\');
                lBuilder.Append(lChar);
            }

            lBuilder.Append('"');

            return lBuilder.ToString();
        }
    }
}