using System;
using System.Collections.Generic;
using System.Text;

namespace work.bacome.imapclient.support
{
    public class cCommandPartFactory
    {
        public static readonly cCommandPartFactory Validation = new cCommandPartFactory(false, null);

        private static readonly cBytes UTC = new cBytes("+0000");

        public readonly bool UTF8Enabled;
        public readonly Encoding Encoding;
        public readonly cCommandPart CharsetName;

        public cCommandPartFactory(bool pUTF8Enabled, Encoding pEncoding)
        {
            UTF8Enabled = pUTF8Enabled;

            if (pUTF8Enabled) CharsetName = null;
            else
            {
                Encoding = pEncoding;
                if (pEncoding == null) CharsetName = null;
                else if (!TryAsAtom(pEncoding.WebName, out CharsetName) && !ZTryAsQuotedASCII(pEncoding.WebName, false, out CharsetName)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            }
        }

        public bool TryAsLiteral(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (ZTryAsASCIILiteral(pString, pSecret, out rResult)) return true;
            if (Encoding == null) { rResult = null; return false; }
            return ZTryAsEncodedLiteral(pString, pSecret, out rResult);
        }

        public cCommandPart AsLiteral(string pString, bool pSecret = false)
        {
            if (TryAsLiteral(pString, pSecret, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public bool TryAsString(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            if (UTF8Enabled)
            {
                if (ZTryAsQuotedUTF8(pString, pSecret, out rResult)) return true;
                return (ZTryAsUTF8Literal(pString, pSecret, out rResult));
            }
            else if (Encoding == null)
            {
                if (ZTryAsQuotedASCII(pString, pSecret, out rResult)) return true;
                return (ZTryAsASCIILiteral(pString, pSecret, out rResult));
            }
            else
            {
                if (ZTryAsQuotedASCII(pString, pSecret, out rResult)) return true;
                var lBytes = Encoding.GetBytes(pString);
                if (ZTryAsQuotedASCII(lBytes, true, pSecret, out rResult)) return true;
                return (ZTryAsLiteral(lBytes, true, pSecret, out rResult));
            }
        }

        public cCommandPart AsString(string pString, bool pSecret = false)
        {
            if (TryAsString(pString, pSecret, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public bool TryAsAString(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            if (ZTryAsBytesInCharset(pString, cCharset.AString, pSecret, out rResult)) return true;

            if (Encoding == null) return TryAsString(pString, pSecret, out rResult);

            var lBytes = Encoding.GetBytes(pString);
            if (ZTryAsBytesInCharset(lBytes, cCharset.AString, true, pSecret, out rResult)) return true;
            if (ZTryAsQuotedASCII(lBytes, true, pSecret, out rResult)) return true;
            return (ZTryAsLiteral(lBytes, true, pSecret, out rResult));
        }

        public cCommandPart AsAString(string pString, bool pSecret = false)
        {
            if (TryAsAString(pString, pSecret, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public bool TryAsNString(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null)
            {
                if (pSecret) rResult = cCommandPart.NilSecret;
                else rResult = cCommandPart.Nil;
                return true;
            }

            return TryAsString(pString, pSecret, out rResult);
        }

        public cCommandPart AsNString(string pString, bool pSecret = false)
        {
            if (TryAsNString(pString, pSecret, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public bool TryAsListMailbox(string pString, char? pDelimiter, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }
            return ZTryAsMailbox(pString, pDelimiter, cCharset.ListMailbox, out rResult, out _);
        }

        public cCommandPart AsListMailbox(string pString, char? pDelimiter)
        {
            if (string.IsNullOrEmpty(pString)) throw new ArgumentOutOfRangeException(nameof(pString));
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));
            if (!ZTryAsMailbox(pString, pDelimiter, cCharset.ListMailbox, out var lResult, out _)) throw new ArgumentOutOfRangeException(nameof(pString));
            return lResult;
        }

        /*
        public bool TryAsMailbox(string pString, char? pDelimiter, out cCommandPart rCommandPart, out string rEncodedString)
        {
            if (string.IsNullOrEmpty(pString)) { rCommandPart = null; rEncodedString = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rCommandPart = null; rEncodedString = null; return false; }
            return ZTryAsMailbox(pString, pDelimiter, cCharset.AString, out rCommandPart, out rEncodedString);
        } */

        public bool TryAsMailbox(cMailboxName pMailboxName, out cCommandPart rCommandPart, out string rEncodedMailboxName)
        {
            if (pMailboxName == null) { rCommandPart = null; rEncodedMailboxName = null; return false; }
            return ZTryAsMailbox(pMailboxName.Name, pMailboxName.Delimiter, cCharset.AString, out rCommandPart, out rEncodedMailboxName);
        }

        /*
        public cCommandPart AsMailbox(string pString, char? pDelimiter)
        {
            if (string.IsNullOrEmpty(pString)) throw new ArgumentOutOfRangeException(nameof(pString));
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));
            if (!ZTryAsMailbox(pString, pDelimiter, cCharset.AString, out var lResult, out _)) throw new ArgumentOutOfRangeException(nameof(pString));
            return lResult;
        } */

        private bool ZTryAsMailbox(string pString, char? pDelimiter, cCharset pCharset, out cCommandPart rCommandPart, out string rEncodedMailboxName)
        {
            if (pString.Equals(cMailboxName.InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                rCommandPart = new cCommandPart(cMailboxName.InboxBytes);
                rEncodedMailboxName = cMailboxName.InboxString;
            }

            if (ZTryAsBytesInCharset(pString, pCharset, false, out rCommandPart))
            {
                rEncodedMailboxName = pString;
                return true;
            }

            if (UTF8Enabled)
            {
                if (ZTryAsQuotedUTF8(pString, false, out rCommandPart) || ZTryAsUTF8Literal(pString, false, out rCommandPart))
                {
                    rEncodedMailboxName = pString;
                    return true;
                }

                rEncodedMailboxName = null;
                return false;
            }

            cByteList lBytes;

            if (pDelimiter == null) lBytes = cModifiedUTF7.Encode(pString);
            else
            {
                char lDelimiterChar = pDelimiter.Value;
                byte lDelimiterByte = (byte)lDelimiterChar;

                string[] lSegments = pString.Split(lDelimiterChar);

                lBytes = new cByteList();
                bool lFirst = true;

                foreach (string lSegment in lSegments)
                {
                    if (lFirst) lFirst = false;
                    else lBytes.Add(lDelimiterByte);
                    lBytes.AddRange(cModifiedUTF7.Encode(lSegment));
                }
            }

            if (ZTryAsBytesInCharset(lBytes, pCharset, false, false, out rCommandPart) || ZTryAsQuotedASCII(lBytes, false, false, out rCommandPart) || ZTryAsASCIILiteral(lBytes, false, out rCommandPart))
            {
                rEncodedMailboxName = cTools.ASCIIBytesToString(lBytes);
                return true;
            }

            rEncodedMailboxName = null;
            return false;
        }

        private bool ZTryAsEncodedLiteral(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }
            var lBytes = Encoding.GetBytes(pString);
            foreach (byte lByte in lBytes) if (lByte == cASCII.NUL) { rResult = null; return false; }
            rResult = new cCommandPart(lBytes, eCommandPartType.literal, pSecret, true);
            return true;
        }

        public static cCommandPart AsDate(DateTime pDate)
        {
            var lBytes = new cByteList(11);

            lBytes.AddRange(ZIntToBytes(pDate.Day, 0));
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(cASCIIMonth.Name[pDate.Month - 1]);
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(ZIntToBytes(pDate.Year, 4));

            return new cCommandPart(lBytes);
        }

        public static cCommandPart AsDateTime(DateTime pDate)
        {
            var lBytes = new cByteList(26);

            lBytes.Add(cASCII.DQUOTE);

            lBytes.AddRange(ZIntToBytes(pDate.Day, 2));
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(cASCIIMonth.Name[pDate.Month - 1]);
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(ZIntToBytes(pDate.Year, 4));

            lBytes.Add(cASCII.SPACE);

            lBytes.AddRange(ZIntToBytes(pDate.Hour, 2));
            lBytes.Add(cASCII.COLON);
            lBytes.AddRange(ZIntToBytes(pDate.Minute, 2));
            lBytes.Add(cASCII.COLON);
            lBytes.AddRange(ZIntToBytes(pDate.Second, 2));

            lBytes.Add(cASCII.SPACE);

            if (pDate.Kind == DateTimeKind.Utc) lBytes.AddRange(UTC);
            else
            {
                var lOffset = TimeZoneInfo.Local.GetUtcOffset(pDate);

                if (lOffset < TimeSpan.Zero)
                {
                    lBytes.Add(cASCII.HYPEN);
                    lOffset = -lOffset;
                }
                else lBytes.Add(cASCII.PLUS);

                var lOffsetChars = lOffset.ToString("hhmm");

                foreach (var lChar in lOffsetChars) lBytes.Add((byte)lChar);
            }

            return new cCommandPart(lBytes);
        }

        public static cCommandPart AsLiteral8(IList<byte> pBytes) => new cCommandPart(pBytes, eCommandPartType.literal8);

        public static bool TryAsAtom(string pString, out cCommandPart rResult) => ZTryAsBytesInCharset(pString, cCharset.Atom, false, out rResult);

        public static cCommandPart AsAtom(string pString)
        {
            if (ZTryAsBytesInCharset(pString, cCharset.Atom, false, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        ;?;
        // <<header is an astring without the option to encode>> => as asciiastring
        public static bool TryAsRFC822HeaderField(string pString, out cCommandPart rResult) => ZTryAsBytesInCharset(pString, cCharset.RFC822HeaderField, false, out rResult);

        public static cCommandPart AsRFC822HeaderField(string pString)
        {
            if (ZTryAsBytesInCharset(pString, cCharset.RFC822HeaderField, false, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        /*
        public static bool TryAsCharsetName(string pString, out cCommandPart rResult)
        {
            // rfc 3501 says a charset can be an astring; rfc 5256 says a charset can be an atom or a quoted string
            //  I'm going with the latter here ...
            //
            if (TryAsAtom(pString, out rResult)) return true;
            if (ZTryAsQuotedASCII(pString, false, out rResult)) return true;
            return false;
        }

        public static cCommandPart AsCharsetName(string pString)
        {
            if (TryAsCharsetName(pString, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        } */

        private static cByteList ZIntToBytes(int pNumber, int pMinLength)
        {
            if (pNumber < 0) throw new ArgumentOutOfRangeException(nameof(pNumber));
            cByteList lBytes = cTools.IntToBytesReverse(pNumber);
            for (int i = lBytes.Count; i < pMinLength; i++) lBytes.Add(cASCII.ZERO);
            lBytes.Reverse();
            return lBytes;
        }

        private static bool ZTryAsBytesInCharset(string pString, cCharset pCharset, bool pSecret, out cCommandPart rResult)
        {
            if (string.IsNullOrEmpty(pString)) { rResult = null; return false; }

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (!pCharset.Contains(lChar)) { rResult = null; return false; }
                lBytes.Add((byte)lChar);
            }

            rResult = new cCommandPart(lBytes, eCommandPartType.text, pSecret);
            return true;
        }

        private static bool ZTryAsBytesInCharset(IList<byte> pBytes, cCharset pCharset, bool pSecret, bool pEncoded, out cCommandPart rResult)
        {
            if (pBytes == null || pBytes.Count == 0) { rResult = null; return false; }

            foreach (byte lByte in pBytes) if (!pCharset.Contains(lByte)) { rResult = null; return false; }

            rResult = new cCommandPart(pBytes, eCommandPartType.text, pSecret, pEncoded);
            return true;
        }

        private static bool ZTryAsQuotedASCII(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            var lBytes = new cByteList(pString.Length + 2);

            lBytes.Add(cASCII.DQUOTE);

            foreach (char lChar in pString)
            {
                if (lChar == cChar.NUL || lChar == cChar.CR || lChar == cChar.LF || lChar > cChar.DEL) { rResult = null; return false; }
                if (lChar == '"' || lChar == '\\') lBytes.Add(cASCII.BACKSL);
                lBytes.Add((byte)lChar);
            }

            lBytes.Add(cASCII.DQUOTE);

            rResult = new cCommandPart(lBytes, eCommandPartType.text, pSecret);
            return true;
        }

        private static bool ZTryAsQuotedASCII(IList<byte> pBytes, bool pSecret, bool pEncoded, out cCommandPart rResult)
        {
            if (pBytes == null) { rResult = null; return false; }

            var lBytes = new cByteList(pBytes.Count + 2);

            lBytes.Add(cASCII.DQUOTE);

            foreach (byte lByte in pBytes)
            {
                if (lByte == cASCII.NUL || lByte == cASCII.CR || lByte == cASCII.LF || lByte > cASCII.DEL) { rResult = null; return false; }
                if (lByte == cASCII.DQUOTE || lByte == cASCII.BACKSL) lBytes.Add(cASCII.BACKSL);
                lBytes.Add(lByte);
            }

            lBytes.Add(cASCII.DQUOTE);

            rResult = new cCommandPart(pBytes, eCommandPartType.text, pSecret, pEncoded);
            return true;
        }

        private static bool ZTryAsLiteral(IList<byte> pBytes, bool pSecret, bool pEncoded, out cCommandPart rResult)
        {
            if (pBytes == null) { rResult = null; return false; }
            foreach (byte lByte in pBytes) if (lByte == cASCII.NUL) { rResult = null; return false; }
            rResult = new cCommandPart(pBytes, eCommandPartType.literal, pSecret, pEncoded);
            return true;
        }

        private static bool ZTryAsASCIILiteral(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (lChar == cChar.NUL || lChar > cChar.DEL) { rResult = null; return false; }
                lBytes.Add((byte)lChar);
            }

            rResult = new cCommandPart(lBytes, eCommandPartType.literal, pSecret);
            return true;
        }

        private static bool ZTryAsASCIILiteral(IList<byte> pBytes, bool pSecret, out cCommandPart rResult)
        {
            if (pBytes == null) { rResult = null; return false; }
            foreach (byte lByte in pBytes) if (lByte == cASCII.NUL || lByte > cASCII.DEL) { rResult = null; return false; }
            rResult = new cCommandPart(pBytes, eCommandPartType.literal, pSecret);
            return true;
        }

        private static bool ZTryAsUTF8Literal(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }
            var lBytes = Encoding.UTF8.GetBytes(pString);
            foreach (byte lByte in lBytes) if (lByte == cASCII.NUL) { rResult = null; return false; }
            rResult = new cCommandPart(lBytes, eCommandPartType.literal, pSecret);
            return true;
        }

        private static bool ZTryAsQuotedUTF8(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            var lEncBytes = Encoding.UTF8.GetBytes(pString);

            var lBytes = new cByteList(lEncBytes.Length + 2);

            lBytes.Add(cASCII.DQUOTE);

            foreach (byte lByte in lEncBytes)
            {
                if (lByte == cASCII.NUL || lByte == cASCII.CR || lByte == cASCII.LF) { rResult = null; return false; }
                if (lByte == cASCII.DQUOTE || lByte == cASCII.BACKSL) lBytes.Add(cASCII.BACKSL);
                lBytes.Add(lByte);
            }

            lBytes.Add(cASCII.DQUOTE);

            rResult = new cCommandPart(lBytes, eCommandPartType.text, pSecret);
            return true;
        }
    }
}