using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace work.bacome.imapinternals
{
    public static class cTools
    {
        private static readonly IdnMapping kIDNMapping = new IdnMapping();
        private static readonly char[] kWSP = new char[] { '\t', ' ' };

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

        public static List<byte> UIntToBytesReverse(uint pNumber)
        {
            var lBytes = new List<byte>(10);

            uint lNumber = pNumber;

            do
            {
                int lDigit = (int)(lNumber % 10);
                lBytes.Add((byte)(cASCII.ZERO + lDigit));
                lNumber = lNumber / 10;
            } while (lNumber > 0);

            return lBytes;
        }

        public static List<byte> ULongToBytesReverse(ulong pNumber)
        {
            var lBytes = new List<byte>(20);

            ulong lNumber = pNumber;

            do
            {
                int lDigit = (int)(lNumber % 10);
                lBytes.Add((byte)(cASCII.ZERO + lDigit));
                lNumber = lNumber / 10;
            } while (lNumber > 0);

            return lBytes;
        }

        public static List<byte> IntToBytesReverse(int pNumber)
        {
            if (pNumber < 0) throw new ArgumentOutOfRangeException(nameof(pNumber));

            var lBytes = new List<byte>(10);

            int lNumber = pNumber;

            do
            {
                int lDigit = lNumber % 10;
                lBytes.Add((byte)(cASCII.ZERO + lDigit));
                lNumber = lNumber / 10;
            } while (lNumber > 0);

            return lBytes;
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

        public static char[] ByteToHexChars(byte pByte)
        {
            char[] lResult = new char[2];
            lResult[0] = LHexDigit(pByte >> 4);
            lResult[1] = LHexDigit(pByte & 0b1111);
            return lResult;

            char LHexDigit(int pNibble)
            {
                if (pNibble < 10) return (char)('0' + pNibble);
                return (char)('A' + pNibble - 10);
            }
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

        public static bool ContainsNonASCII(IEnumerable<char> pChars)
        {
            foreach (var lChar in pChars) if (lChar > cChar.DEL) return true;
            return false;
        }

        public static bool ContainsWSP(string pString)
        {
            if (pString == null) return false;
            return pString.IndexOfAny(kWSP) != -1;
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