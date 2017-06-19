using System;
using System.Collections.Generic;
using System.Text;

namespace work.bacome.imapclient.support
{
    public class cCommandPart
    {
        [Flags]
        private enum fType
        {
            plaintext = 0,
            literal8 = 1,
            literal = 1 << 1,
            encoded = 1 << 2,
            secret = 1 << 3
        }

        public static readonly cCommandPart Space = new cCommandPart(" ");
        public static readonly cCommandPart Nil = new cCommandPart("NIL");
        public static readonly cCommandPart SecretNil = new cCommandPart("NIL", true);
        public static readonly cCommandPart LParen = new cCommandPart("(");
        public static readonly cCommandPart RParen = new cCommandPart(")");
        public static readonly cCommandPart RBracket = new cCommandPart("]");

        private static readonly cBytes UTC = new cBytes("+0000");

        private readonly fType mType;
        public readonly cBytes Bytes;
        public readonly cBytes LiteralLengthBytes;

        private cCommandPart(fType pType, IList<byte> pBytes)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            mType = pType;
            Bytes = new cBytes(pBytes);
            if ((mType & (fType.literal8 | fType.literal)) == 0) LiteralLengthBytes = null;
            else LiteralLengthBytes = new cBytes(ZIntToBytes(pBytes.Count, 0));
        }

        public cCommandPart(string pString, bool pSecret = false)
        {
            if (string.IsNullOrEmpty(pString)) throw new ArgumentOutOfRangeException(nameof(pString));

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (lChar < ' ' || lChar > '~') throw new ArgumentOutOfRangeException(nameof(pString));
                lBytes.Add((byte)lChar);
            }

            mType = pSecret ? fType.secret : fType.plaintext;
            Bytes = new cBytes(lBytes);
            LiteralLengthBytes = null;
        }

        public cCommandPart(IList<byte> pBytes, bool pSecret = false)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));

            foreach (byte lByte in pBytes) if (lByte < cASCII.SPACE || lByte > cASCII.TILDA) throw new ArgumentOutOfRangeException(nameof(pBytes));

            mType = pSecret ? fType.secret : fType.plaintext;
            Bytes = new cBytes(pBytes);
            LiteralLengthBytes = null;
        }

        public cCommandPart(uint pNumber, bool pSecret = false)
        {
            var lBytes = cTools.UIntToBytesReverse(pNumber);
            lBytes.Reverse();

            mType = pSecret ? fType.secret : fType.plaintext;
            Bytes = new cBytes(lBytes);
            LiteralLengthBytes = null;
        }

        public cCommandPart(cSequenceSet pSequenceSet, bool pSecret = false)
        {
            cByteList lBytes = new cByteList();
            cByteList lTemp = new cByteList();

            bool lFirst = true;

            foreach (var lItem in pSequenceSet)
            {
                if (lFirst) lFirst = false;
                else lBytes.Add(cASCII.COMMA);

                if (lItem == cSequenceSet.cItem.Asterisk)
                {
                    lBytes.Add(cASCII.ASTERISK);
                    continue;
                }

                if (lItem is cSequenceSet.cItem.cNumber lNumber)
                {
                    lTemp = cTools.UIntToBytesReverse(lNumber.Number);
                    lTemp.Reverse();
                    lBytes.AddRange(lTemp);
                    continue;
                }

                if (!(lItem is cSequenceSet.cItem.cRange lRange)) throw new ArgumentException("invalid form 1", nameof(pSequenceSet));

                if (lRange.From == cSequenceSet.cItem.Asterisk)
                {
                    lBytes.Add(cASCII.ASTERISK);
                    continue;
                }

                if (!(lRange.From is cSequenceSet.cItem.cNumber lFrom)) throw new ArgumentException("invalid form 2", nameof(pSequenceSet));

                lTemp = cTools.UIntToBytesReverse(lFrom.Number);
                lTemp.Reverse();
                lBytes.AddRange(lTemp);

                lBytes.Add(cASCII.COLON);

                if (lRange.To == cSequenceSet.cItem.Asterisk)
                {
                    lBytes.Add(cASCII.ASTERISK);
                    continue;
                }

                if (!(lRange.To is cSequenceSet.cItem.cNumber lTo)) throw new ArgumentException("invalid form 3", nameof(pSequenceSet));

                lTemp = cTools.UIntToBytesReverse(lTo.Number);
                lTemp.Reverse();
                lBytes.AddRange(lTemp);
            }

            mType = pSecret ? fType.secret : fType.plaintext;
            Bytes = new cBytes(lBytes);
            LiteralLengthBytes = null;
        }

        public bool Literal8 => (mType & fType.literal8) != 0;
        public bool Literal => (mType & fType.literal) != 0;
        public bool Encoded => (mType & fType.encoded) != 0;
        public bool Secret => (mType & fType.secret) != 0;

        public static cCommandPart AsDate(DateTime pDate, bool pSecret = false)
        {
            var lBytes = new cByteList(11);

            lBytes.AddRange(ZIntToBytes(pDate.Day, 0));
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(cASCIIMonth.Name[pDate.Month - 1]);
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(ZIntToBytes(pDate.Year, 4));

            return new cCommandPart(pSecret ? fType.secret : fType.plaintext, lBytes);
        }

        public static cCommandPart AsDateTime(DateTime pDate, bool pSecret = false)
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

            return new cCommandPart(pSecret ? fType.secret : fType.plaintext, lBytes);
        }

        public static cCommandPart AsLiteral8(IList<byte> pBytes) => new cCommandPart(fType.literal8, pBytes);

        public static bool TryAsAtom(string pString, out cCommandPart rResult) => ZTryAsBytesInCharset(pString, cCharset.Atom, false, out rResult);

        public static cCommandPart AsAtom(string pString)
        {
            if (ZTryAsBytesInCharset(pString, cCharset.Atom, false, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public static bool TryAsRFC822HeaderField(string pString, out cCommandPart rResult) => ZTryAsBytesInCharset(pString, cCharset.RFC822HeaderField, false, out rResult);

        public static cCommandPart AsRFC822HeaderField(string pString)
        {
            if (ZTryAsBytesInCharset(pString, cCharset.RFC822HeaderField, false, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

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
        }

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

            rResult = new cCommandPart(pSecret ? fType.secret : fType.plaintext, lBytes);
            return true;
        }

        private static bool ZTryAsBytesInCharset(IList<byte> pBytes, cCharset pCharset, bool pEncoded, bool pSecret, out cCommandPart rResult)
        {
            if (pBytes == null || pBytes.Count == 0) { rResult = null; return false; }

            foreach (byte lByte in pBytes) if (!pCharset.Contains(lByte)) { rResult = null; return false; }

            fType lType = 0;
            if (pEncoded) lType |= fType.encoded;
            if (pSecret) lType |= fType.secret;

            rResult = new cCommandPart(lType, pBytes);
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

            rResult = new cCommandPart(pSecret ? fType.secret : fType.plaintext, lBytes);
            return true;
        }

        private static bool ZTryAsQuotedASCII(IList<byte> pBytes, bool pEncoded, bool pSecret, out cCommandPart rResult)
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

            fType lType = 0;
            if (pEncoded) lType |= fType.encoded;
            if (pSecret) lType |= fType.secret;

            rResult = new cCommandPart(lType, lBytes);
            return true;
        }

        private static bool ZTryAsLiteral(IList<byte> pBytes, bool pEncoded, bool pSecret, out cCommandPart rResult)
        {
            if (pBytes == null) { rResult = null; return false; }
            foreach (byte lByte in pBytes) if (lByte == cASCII.NUL) { rResult = null; return false; }

            fType lType = fType.literal;
            if (pEncoded) lType |= fType.encoded;
            if (pSecret) lType |= fType.secret;

            rResult = new cCommandPart(lType, pBytes);
            return true;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCommandPart));

            if ((mType & fType.secret) == 0)
            {
                lBuilder.Append(mType);
                lBuilder.Append(Bytes);
            }
            else lBuilder.Append("secret");

            return lBuilder.ToString();
        }

        public class cFactory
        {
            private bool mEnabledUTF8;
            private Encoding mEncoding;

            public cFactory()
            {
                mEnabledUTF8 = false;
                mEncoding = null;
            }

            public cFactory(bool pEnabledUTF8)
            {
                mEnabledUTF8 = pEnabledUTF8;
                mEncoding = null;
            }

            public cFactory(Encoding pEncoding)
            {
                mEnabledUTF8 = false;
                mEncoding = pEncoding;
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

                rResult = new cCommandPart(pSecret ? fType.literal | fType.secret : fType.literal, lBytes);
                return true;
            }

            private static bool ZTryAsASCIILiteral(IList<byte> pBytes, bool pSecret, out cCommandPart rResult)
            {
                if (pBytes == null) { rResult = null; return false; }
                foreach (byte lByte in pBytes) if (lByte == cASCII.NUL || lByte > cASCII.DEL) { rResult = null; return false; }
                rResult = new cCommandPart(pSecret ? fType.literal | fType.secret : fType.literal, pBytes);
                return true;
            }

            private static bool ZTryAsUTF8Literal(string pString, bool pSecret, out cCommandPart rResult)
            {
                if (pString == null) { rResult = null; return false; }
                var lBytes = Encoding.UTF8.GetBytes(pString);
                foreach (byte lByte in lBytes) if (lByte == cASCII.NUL) { rResult = null; return false; }
                rResult = new cCommandPart(pSecret ? fType.literal | fType.secret : fType.literal, lBytes);
                return true;
            }

            private bool ZTryAsEncodedLiteral(string pString, bool pSecret, out cCommandPart rResult)
            {
                if (pString == null) { rResult = null; return false; }
                var lBytes = mEncoding.GetBytes(pString);
                foreach (byte lByte in lBytes) if (lByte == cASCII.NUL) { rResult = null; return false; }
                rResult = new cCommandPart(pSecret ? fType.literal | fType.encoded | fType.secret : fType.literal | fType.encoded, lBytes);
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

                rResult = new cCommandPart(pSecret ? fType.secret : fType.plaintext, lBytes);
                return true;
            }

            public bool TryAsLiteral(string pString, bool pSecret, out cCommandPart rResult)
            {
                if (ZTryAsASCIILiteral(pString, pSecret, out rResult)) return true;
                if (mEncoding == null) { rResult = null; return false; }
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

                if (mEnabledUTF8)
                {
                    if (ZTryAsQuotedUTF8(pString, pSecret, out rResult)) return true;
                    return (ZTryAsUTF8Literal(pString, pSecret, out rResult));
                }
                else if (mEncoding == null)
                {
                    if (ZTryAsQuotedASCII(pString, pSecret, out rResult)) return true;
                    return (ZTryAsASCIILiteral(pString, pSecret, out rResult));
                }
                else
                {
                    if (ZTryAsQuotedASCII(pString, pSecret, out rResult)) return true;
                    var lBytes = mEncoding.GetBytes(pString);
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

                if (mEncoding == null) return TryAsString(pString, pSecret, out rResult);

                var lBytes = mEncoding.GetBytes(pString);
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
                    if (pSecret) rResult = SecretNil;
                    else rResult = Nil;
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

            public bool TryAsMailbox(cMailboxName pMailboxName, out cCommandPart rCommandPart, out string rEncodedString)
            {
                if (pMailboxName == null) { rCommandPart = null; rEncodedString = null; return false; }
                return ZTryAsMailbox(pMailboxName.Name, pMailboxName.Delimiter, cCharset.AString, out rCommandPart, out rEncodedString);
            }

            /*
            public cCommandPart AsMailbox(string pString, char? pDelimiter)
            {
                if (string.IsNullOrEmpty(pString)) throw new ArgumentOutOfRangeException(nameof(pString));
                if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));
                if (!ZTryAsMailbox(pString, pDelimiter, cCharset.AString, out var lResult, out _)) throw new ArgumentOutOfRangeException(nameof(pString));
                return lResult;
            } */

            private bool ZTryAsMailbox(string pString, char? pDelimiter, cCharset pCharset, out cCommandPart rCommandPart, out string rEncodedString)
            {
                if (pString.Equals(cMailboxName.InboxString, StringComparison.InvariantCultureIgnoreCase))
                {
                    rCommandPart = new cCommandPart(fType.plaintext, cMailboxName.InboxBytes);
                    rEncodedString = cMailboxName.InboxString;
                }

                if (ZTryAsBytesInCharset(pString, pCharset, false, out rCommandPart))
                {
                    rEncodedString = pString;
                    return true;
                }

                if (mEnabledUTF8)
                {
                    if (ZTryAsQuotedUTF8(pString, false, out rCommandPart) || ZTryAsUTF8Literal(pString, false, out rCommandPart))
                    {
                        rEncodedString = pString;
                        return true;
                    }

                    rEncodedString = null;
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
                    rEncodedString = cTools.ASCIIBytesToString(lBytes);
                    return true;
                }

                rEncodedString = null;
                return false;
            }
        }
    }
}