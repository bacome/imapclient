using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace work.bacome.imapclient
{
    internal static class cTools
    {
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

        public static string PunycodeBytesToString(IList<byte> pBytes)
        {
            // TODO (hard)
            return ASCIIBytesToString(pBytes);
        }

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

        public static bool MailMessageFormCanBeHandled(MailMessage pMessage, out string rError)
        {
            // checks that the mailmessage is in a form that I can handle
            rError = "not implemented yet";
            return false;
        }
    }
}