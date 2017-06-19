using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using work.bacome.trace;

namespace work.bacome.imapclient.support
{
    public static class cTools
    {
        private const char kLCHEVRON = '\u00AB';
        private const char kRCHEVRON = '\u00BB';

        public static string ASCIIBytesToString(IList<byte> pBytes)
        {
            if (pBytes.Count == 0) return string.Empty;
            char[] lChars = new char[pBytes.Count];
            for (int i = 0; i < pBytes.Count; i++) lChars[i] = (char)pBytes[i];
            return new string(lChars);
        }

        public static string ASCIIBytesToString(byte pPrefix, IList<byte> pBytes)
        {
            char[] lChars = new char[pBytes.Count + 1];
            lChars[0] = (char)pPrefix;
            for (int i = 0, j = 1; i < pBytes.Count; i++, j++) lChars[j] = (char)pBytes[i];
            return new string(lChars);
        }

        public static string UTF8BytesToString(IList<byte> pBytes)
        {
            if (pBytes.Count == 0) return string.Empty;
            byte[] lBytes = new byte[pBytes.Count];
            for (int i = 0; i < pBytes.Count; i++) lBytes[i] = pBytes[i];
            return new string(Encoding.UTF8.GetChars(lBytes));
        }

        public static string BytesToLoggableString(IList<byte> pBytes)
        {
            StringBuilder lBuilder = new StringBuilder();

            foreach (byte lByte in pBytes)
            {
                if (lByte < cASCII.SPACE || lByte > cASCII.TILDA)
                {
                    lBuilder.Append(kLCHEVRON);
                    lBuilder.Append(lByte);
                    lBuilder.Append(kRCHEVRON);
                }
                else lBuilder.Append((char)lByte);
            }

            return lBuilder.ToString();
        }

        public static string BytesToLoggableString(string pNameOfClass, IList<byte> pBytes)
        {
            StringBuilder lBuilder = new StringBuilder($"{pNameOfClass}(");

            foreach (byte lByte in pBytes)
            {
                if (lByte < cASCII.SPACE || lByte > cASCII.TILDA)
                {
                    lBuilder.Append(kLCHEVRON);
                    lBuilder.Append(lByte);
                    lBuilder.Append(kRCHEVRON);
                }
                else lBuilder.Append((char)lByte);
            }

            return lBuilder.ToString() + ")";
        }

        public static bool TryMailboxNameBytesToString(IList<byte> pBytes, byte? pDelimiter, fEnableableExtensions pEnabledExtensions, out string rString)
        {
            if ((pEnabledExtensions & fEnableableExtensions.utf8) != 0) { rString = UTF8BytesToString(pBytes); return true; }

            if (pDelimiter == null) return cModifiedUTF7.TryDecode(pBytes, out rString, out _);

            byte lDelimiterByte = pDelimiter.Value;
            char lDelimiterChar = (char)lDelimiterByte;

            List<cByteList> lSegments = new List<cByteList>();

            cByteList lSegment = new cByteList();

            foreach (byte lByte in pBytes)
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
            if (pBytes == null) { rString = null; return true; }

            if (pCharset == null) { rString = UTF8BytesToString(pBytes); return true; }

            try
            {
                byte[] lBytes = new byte[pBytes.Count];
                pBytes.CopyTo(lBytes, 0);
                rString = new string(Encoding.GetEncoding(pCharset).GetChars(lBytes));
                return true;
            }
            catch
            {
                rString = null;
                return false;
            }
        }

        public static class cTests
        {
            [Conditional("DEBUG")]
            public static void Tests(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewGeneric($"{nameof(cTools)}.{nameof(cTests)}.{nameof(Tests)}");

                for (int i = 0; i < 1000; i++)
                {
                    var lBytes = IntToBytesReverse(i);

                    int lOutput = 0;

                    for (int j = 0, f = 1; j < lBytes.Count; j++, f *= 10)
                    {
                        lOutput = lOutput + (lBytes[j] - cASCII.ZERO) * f;
                    }

                    if (lOutput != i) throw new cTestsException($"IntToBytesReverse({i}->{lBytes})");
                }
            }
        }
    }
}