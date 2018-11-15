using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapsupport;
using work.bacome.imaptools;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        // APIs that return a string treat the bytes as UTF8 (when in a literal and/or quoted string)
        //  if this is not the desired behaviour then the APIs that return the byte collections should be used
        // APIs that return strings may throw if the bytes aren't valid UTF8

        public static readonly cBytes Nil = new cBytes("NIL");
        public static readonly cBytes RBracketSpace = new cBytes("] ");
        public static readonly cBytes SpaceLParen = new cBytes(" (");
        public static readonly cBytes RParenSpace = new cBytes(") ");

        private cResponse mLines;
        public sPosition Position;

        public cBytesCursor(cResponse pResponse)
        {
            mLines = pResponse ?? throw new ArgumentNullException(nameof(pResponse));

            if (mLines.Count == 0)
            {
                Position.AtEnd = true;
                return;
            }

            Position.BytesLine = mLines[0];
            Position.LineNumber = 0;
            Position.Byte = -1;
            Position.AtEnd = false;

            ZAdvance(ref Position);
        }

        public cBytesCursor(IList<byte> pBytes)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));

            mLines = new cResponse(new cResponseLine[] { new cResponseLine(false, pBytes) });

            Position.BytesLine = mLines[0];
            Position.LineNumber = 0;
            Position.Byte = -1;
            Position.AtEnd = false;

            ZAdvance(ref Position);
        }

        public cBytesCursor(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            mLines = new cResponse(new cResponseLine[] { new cResponseLine(false, Encoding.UTF8.GetBytes(pString)) });

            Position.BytesLine = mLines[0];
            Position.LineNumber = 0;
            Position.Byte = -1;
            Position.AtEnd = false;

            ZAdvance(ref Position);
        }

        public bool SkipByte(byte pByte, bool pCaseSensitive = false)
        {
            if (Position.AtEnd) return false;
            if (Position.BytesLine.Literal) return false;

            if (cASCII.Compare(Position.BytesLine[Position.Byte], pByte, pCaseSensitive))
            {
                ZAdvance(ref Position);
                return true;
            }

            return false;
        }

        public bool SkipBytes(IList<byte> pBytes, bool pCaseSensitive = false)
        {
            if (Position.AtEnd) return false;
            if (Position.BytesLine.Literal) return false;

            var lBookmark = Position;

            int lByte = 0;

            while (true)
            {
                if (!cASCII.Compare(Position.BytesLine[Position.Byte], pBytes[lByte], pCaseSensitive))
                {
                    Position = lBookmark;
                    return false;
                }

                ZAdvance(ref Position);
                lByte = lByte + 1;

                if (lByte == pBytes.Count) return true;

                if (Position.AtEnd || Position.BytesLine.Literal)
                {
                    Position = lBookmark;
                    return false;
                }
            }
        }

        public bool GetByte(out byte rByte)
        {
            if (Position.AtEnd) { rByte = 0; return false; }
            if (Position.BytesLine.Literal) { rByte = 0; return false; }
            rByte = Position.BytesLine[Position.Byte];
            ZAdvance(ref Position);
            return true;
        }

        public bool GetString(out string rString)
        {
            if (Position.AtEnd) { rString = null; return false; }

            if (Position.BytesLine.Literal)
            {
                rString = cTools.UTF8BytesToString(Position.BytesLine);
                ZAdvancePart(ref Position);
                return true;
            }

            return GetQuoted(out rString);
        }

        public bool GetString(out IList<byte> rBytes)
        {
            if (Position.AtEnd) { rBytes = null; return false; }

            if (Position.BytesLine.Literal)
            {
                rBytes = Position.BytesLine;
                ZAdvancePart(ref Position);
                return true;
            }

            if (GetQuoted(out cByteList lBytes))
            {
                rBytes = lBytes;
                return true;
            }

            rBytes = null;
            return false;
        }

        public bool GetAString(out string rString)
        {
            if (GetToken(cCharset.AString, null, null, out cByteList lBytes))
            {
                rString = cTools.UTF8BytesToString(lBytes);
                return true;
            }

            return GetString(out rString);
        }

        public bool GetAString(out IList<byte> rBytes)
        {
            if (GetToken(cCharset.AString, null, null, out cByteList lBytes)) { rBytes = lBytes; return true; }
            return GetString(out rBytes);
        }

        public bool GetNString(out string rString)
        {
            if (Position.AtEnd) { rString = null; return false; }

            if (Position.BytesLine.Literal)
            {
                rString = cTools.UTF8BytesToString(Position.BytesLine);
                ZAdvancePart(ref Position);
                return true;
            }

            if (SkipBytes(Nil))
            {
                rString = null;
                return true;
            }

            return GetQuoted(out rString);
        }

        public bool GetNString(out IList<byte> rBytes)
        {
            if (Position.AtEnd) { rBytes = null; return false; }

            if (Position.BytesLine.Literal)
            {
                rBytes = Position.BytesLine;
                ZAdvancePart(ref Position);
                return true;
            }

            if (SkipBytes(Nil))
            {
                rBytes = null;
                return true;
            }

            if (GetQuoted(out cByteList lBytes))
            {
                rBytes = lBytes;
                return true;
            }

            rBytes = null;
            return false;
        }

        public bool GetANString(out string rString)
        {
            if (Position.AtEnd) { rString = null; return false; }

            if (Position.BytesLine.Literal)
            {
                rString = cTools.UTF8BytesToString(Position.BytesLine);
                ZAdvancePart(ref Position);
                return true;
            }

            if (SkipBytes(Nil))
            {
                rString = null;
                return true;
            }

            if (GetToken(cCharset.AString, null, null, out cByteList lBytes))
            {
                rString = cTools.UTF8BytesToString(lBytes);
                return true;
            }

            return GetQuoted(out rString);
        }

        public bool GetQuoted(out string rString)
        {
            if (!GetQuoted(out cByteList lBytes)) { rString = null; return false; }
            rString = cTools.UTF8BytesToString(lBytes);
            return true;
        }

        public bool GetQuoted(out cByteList rBytes)
        {
            var lBookmark = Position;

            if (!SkipByte(cASCII.DQUOTE)) { rBytes = null; return false; }

            rBytes = new cByteList();
            bool lInQuote = false;

            while (true)
            {
                byte lByte = Position.BytesLine[Position.Byte];
                ZAdvance(ref Position);

                if (lInQuote)
                {
                    rBytes.Add(lByte);
                    lInQuote = false;
                }
                else
                {
                    if (lByte == cASCII.DQUOTE) return true;

                    if (lByte == cASCII.BACKSL) lInQuote = true;
                    else rBytes.Add(lByte);
                }

                if (Position.AtEnd || Position.BytesLine.Literal)
                {
                    Position = lBookmark;
                    rBytes = null;
                    return false;
                }
            }
        }

        public bool GetMailboxDelimiter(out byte? rDelimiter)
        {
            if (SkipBytes(Nil))
            {
                rDelimiter = null;
                return true;
            }

            if (!GetQuoted(out cByteList lDelimiters) || lDelimiters.Count != 1) { rDelimiter = null; return false; }

            byte lDelimiter = lDelimiters[0];

            if (cTools.IsValidDelimiter(lDelimiter)) { rDelimiter = lDelimiter; return true; }

            rDelimiter = null;
            return false;
        }

        public bool GetToken(cCharset pCharset, byte? pEncodedIntro, byte? pRepresentsSpace, out cByteList rBytes, int pMinLength = 1, int pMaxLength = int.MaxValue)
        {
            if (Position.AtEnd) { rBytes = null; return false; }
            if (Position.BytesLine.Literal) { rBytes = null; return false; }

            var lBookmark1 = Position;
            rBytes = new cByteList();

            while (true)
            {
                byte lByte = Position.BytesLine[Position.Byte];

                if (lByte == pEncodedIntro)
                {
                    var lBookmark2 = Position;

                    ZAdvance(ref Position);

                    if (ZGetHexEncodedByte(out var lByteFromHex))
                    {
                        rBytes.Add(lByteFromHex);
                        if (Position.AtEnd || Position.BytesLine.Literal || rBytes.Count == pMaxLength) break;
                        continue;
                    }

                    Position = lBookmark2;
                }

                if (lByte == pRepresentsSpace) rBytes.Add(cASCII.SPACE);
                else
                {
                    if (!pCharset.Contains(lByte)) break;
                    rBytes.Add(lByte);
                }

                ZAdvance(ref Position);
                if (Position.AtEnd || Position.BytesLine.Literal || rBytes.Count == pMaxLength) break;
            }

            if (rBytes.Count < pMinLength) { Position = lBookmark1; rBytes = null; return false; }

            return true;
        }

        public bool GetToken(cCharset pCharset, byte? pEncodedIntro, byte? pRepresentsSpace, out string rString)
        {
            var lBookmark = Position;

            if (!GetToken(pCharset, pEncodedIntro, pRepresentsSpace, out cByteList lBytes))
            {
                rString = null;
                return false;
            }

            rString = cTools.UTF8BytesToString(lBytes);
            return true;
        }

        public bool GetNumber(out cByteList rBytes, out uint rNumber, int pMinLength = 1, int pMaxLength = int.MaxValue)
        {
            var lBookmark = Position;

            rNumber = 0;

            if (!GetToken(cCharset.Digit, null, null, out rBytes, pMinLength, pMaxLength)) return false;

            checked
            {
                try { foreach (byte lByte in rBytes) rNumber = rNumber * 10 + lByte - cASCII.ZERO; }
                catch { Position = lBookmark; return false; }
            }

            return true;
        }

        public bool GetNZNumber(out cByteList rBytes, out uint rNumber)
        {
            var lBookmark = Position;

            rNumber = 0;

            if (!GetToken(cCharset.Digit, null, null, out rBytes)) return false;

            if (rBytes[0] == cASCII.ZERO) { Position = lBookmark; return false; }

            checked
            {
                try { foreach (byte lByte in rBytes) rNumber = rNumber * 10 + lByte - cASCII.ZERO; }
                catch { Position = lBookmark; return false; }
            }

            return true;
        }

        public bool GetNumber(out ulong rNumber)
        {
            var lBookmark = Position;

            rNumber = 0;

            if (!GetToken(cCharset.Digit, null, null, out var lBytes)) return false;

            checked
            {
                try { foreach (byte lByte in lBytes) rNumber = rNumber * 10 + lByte - cASCII.ZERO; }
                catch { Position = lBookmark; return false; }
            }

            return true;
        }

        public bool GetDateTime(out cTimestamp rTimestamp)
        {
            // imap date time

            var lBookmark = Position;

            // quote

            if (!SkipByte(cASCII.DQUOTE)) { rTimestamp = null; return false; }

            // date (the day number is either space-digit or digit-digit)

            int lLength;
            if (SkipByte(cASCII.SPACE)) lLength = 1;
            else lLength = 2;

            if (!ZGetDayMonthYear(out var lDay, out var lMonth, out var lYear, lLength, lLength)) { Position = lBookmark; rTimestamp = null; return false; }

            // space

            if (!SkipByte(cASCII.SPACE)) { Position = lBookmark; rTimestamp = null; return false; }

            // time HH:MM:SS

            if (!GetNumber(out _, out uint ltHH, 2, 2) || ltHH > 23) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipByte(cASCII.COLON)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint ltMM, 2, 2) || ltMM > 59) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipByte(cASCII.COLON)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint ltSS, 2, 2) || ltSS > 60) { Position = lBookmark; rTimestamp = null; return false; } // note: 60 is explicitly allowed to cater for leap seconds

            // space

            if (!SkipByte(cASCII.SPACE)) { Position = lBookmark; rTimestamp = null; return false; }

            // zone

            bool lWest;

            if (SkipByte(cASCII.PLUS)) lWest = false;
            else if (SkipByte(cASCII.HYPEN)) lWest = true;
            else { Position = lBookmark; rTimestamp = null; return false; }

            if (!GetNumber(out _, out uint lzHH, 2, 2) || lzHH > 23) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint lzMM, 2, 2) || lzMM > 59) { Position = lBookmark; rTimestamp = null; return false; }

            // generate output

            try
            {
                rTimestamp = new cTimestamp(lYear, lMonth, lDay, (int)ltHH, (int)ltMM, (int)ltSS, 0, lWest, (int)lzHH, (int)lzMM);
                return true;
            }
            catch
            {
                Position = lBookmark;
                rTimestamp = null;
                return false;
            }
        }

        public bool GetTimeStamp(out cTimestamp rTimestamp)
        {
            // rfc3339 timestamp

            var lBookmark = Position;

            // date and time

            if (!GetNumber(out _, out uint ldYYYY, 4, 4)) { rTimestamp = null; return false; }
            if (!SkipByte(cASCII.HYPEN)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint ldMM, 2, 2) || ldMM < 1 || ldMM > 12) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipByte(cASCII.HYPEN)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint ldDD, 2, 2) || ldDD < 1 || ldDD > 31) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipByte(cASCII.T)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint ltHH, 2, 2) || ltHH > 23) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipByte(cASCII.COLON)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint ltMM, 2, 2) || ltMM > 59) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipByte(cASCII.COLON)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!GetNumber(out _, out uint ltSS, 2, 2) || ltSS > 60) { Position = lBookmark; rTimestamp = null; return false; } // note: 60 is explicitly allowed to cater for leap seconds

            // optional milliseconds

            int ltMS = 0;

            if (SkipByte(cASCII.DOT))
            {
                if (!GetToken(cCharset.Digit, null, null, out var lBytes, 1)) { Position = lBookmark; rTimestamp = null; return false; }
                int lDigits = Math.Min(lBytes.Count, 3);
                int lFactor = 100;
                for (int i = 0; i < lDigits; i++, lFactor /= 10) ltMS = ltMS + (lBytes[i] - cASCII.ZERO) * lFactor;
            }

            // zone

            bool lWest;
            uint lzHH;
            uint lzMM;

            if (SkipByte(cASCII.Z))
            {
                lWest = false;
                lzHH = 0;
                lzMM = 0;
            }
            else
            {
                if (SkipByte(cASCII.PLUS)) lWest = false;
                else if (SkipByte(cASCII.HYPEN)) lWest = true;
                else { Position = lBookmark; rTimestamp = null; return false; }

                if (!GetNumber(out _, out lzHH, 2, 2) || lzHH > 23) { Position = lBookmark; rTimestamp = null; return false; }
                if (!SkipByte(cASCII.COLON)) { Position = lBookmark; rTimestamp = null; return false; }
                if (!GetNumber(out _, out lzMM, 2, 2) || lzMM > 59) { Position = lBookmark; rTimestamp = null; return false; }
            }

            // generate output

            try
            {
                rTimestamp = new cTimestamp((int)ldYYYY, (int)ldMM, (int)ldDD, (int)ltHH, (int)ltMM, (int)ltSS, ltMS, lWest, (int)lzHH, (int)lzMM);
                return true;
            }
            catch
            {
                Position = lBookmark;
                rTimestamp = null;
                return false;
            }
        }

        public string GetRestAsString() => new string(Encoding.UTF8.GetChars(ZGetRestAsBytes().ToArray()));

        public IList<byte> GetRestAsBytes() => ZGetRestAsBytes();

        private cByteList ZGetRestAsBytes()
        {
            cByteList lBytes = new cByteList();

            while (!Position.AtEnd)
            {
                for (int i = Position.Byte; i < Position.BytesLine.Count; i++) lBytes.Add(Position.BytesLine[i]);
                Position.Byte = 0;
                ZAdvancePart(ref Position);
            }

            return lBytes;
        }

        public string GetFromAsString(sPosition pFrom) => new string(Encoding.UTF8.GetChars(ZGetFromAsBytes(pFrom).ToArray()));

        public IList<byte> GetFromAsBytes(sPosition pFrom) => ZGetFromAsBytes(pFrom);

        private cByteList ZGetFromAsBytes(sPosition pFrom)
        {
            cByteList lBytes = new cByteList();

            while (pFrom.LineNumber < Position.LineNumber)
            {
                for (int i = pFrom.Byte; i < pFrom.BytesLine.Count; i++) lBytes.Add(pFrom.BytesLine[i]);
                pFrom.Byte = 0;
                ZAdvancePart(ref pFrom);
            }

            if (pFrom.LineNumber == Position.LineNumber)
                for (int i = pFrom.Byte; i < Position.Byte; i++) lBytes.Add(pFrom.BytesLine[i]);

            return lBytes;
        }

        private void ZAdvance(ref sPosition pPosition)
        {
            pPosition.Byte = pPosition.Byte + 1;
            if (pPosition.Byte < pPosition.BytesLine.Count) return;
            pPosition.Byte = 0;
            ZAdvancePart(ref pPosition);
        }

        private void ZAdvancePart(ref sPosition pPosition)
        {
            while (true)
            {
                pPosition.LineNumber = pPosition.LineNumber + 1;

                if (pPosition.LineNumber == mLines.Count)
                {
                    pPosition.AtEnd = true;
                    return;
                }

                pPosition.BytesLine = mLines[pPosition.LineNumber];

                if (pPosition.BytesLine.Count > 0 || pPosition.BytesLine.Literal) return;
            }
        }

        private bool ZGetDayMonthYear(out int rDay, out int rMonth, out int rYear, int pMinDayLength = 1, int pMaxDayLength = 2)
        {
            if (!GetNumber(out _, out uint lDay, pMinDayLength, pMaxDayLength)) { rYear = 0; rMonth = 0; rDay = 0; return false; }
            if (lDay < 1 || lDay > 31) { rYear = 0; rMonth = 0; rDay = 0; return false; }
            rDay = (int)lDay;
            if (!SkipByte(cASCII.HYPEN)) { rYear = 0; rMonth = 0; return false; }
            if (!ZGetMonth(out rMonth)) { rYear = 0; rDay = 0; return false; }
            if (!SkipByte(cASCII.HYPEN)) { rYear = 0; return false; }
            if (!GetNumber(out _, out uint lYear, 4, 4)) { rYear = 0; return false; }
            rYear = (int)lYear;

            return true;
        }

        private bool ZDate(int pYear, int pMonth, int pDay, out DateTime rDate)
        {
            try
            {
                rDate = new DateTime(pYear, pMonth, pDay);
                return true;
            }
            catch
            {
                rDate = new DateTime();
                return false;
            }
        }

        private bool ZGetMonth(out int rMonth)
        {
            if (SkipBytes(cRFCMonth.aJan)) rMonth = 1;
            else if (SkipBytes(cRFCMonth.aFeb)) rMonth = 2;
            else if (SkipBytes(cRFCMonth.aMar)) rMonth = 3;
            else if (SkipBytes(cRFCMonth.aApr)) rMonth = 4;
            else if (SkipBytes(cRFCMonth.aMay)) rMonth = 5;
            else if (SkipBytes(cRFCMonth.aJun)) rMonth = 6;
            else if (SkipBytes(cRFCMonth.aJul)) rMonth = 7;
            else if (SkipBytes(cRFCMonth.aAug)) rMonth = 8;
            else if (SkipBytes(cRFCMonth.aSep)) rMonth = 9;
            else if (SkipBytes(cRFCMonth.aOct)) rMonth = 10;
            else if (SkipBytes(cRFCMonth.aNov)) rMonth = 11;
            else if (SkipBytes(cRFCMonth.aDec)) rMonth = 12;
            else { rMonth = -1; return false; }

            return true;
        }

        private bool ZGetHexEncodedByte(out byte rByte)
        {
            var lBookmark = Position;

            if (ZGetHexEncodedNibble(out int lMSN) && ZGetHexEncodedNibble(out int lLSN))
            {
                rByte = (byte)(lMSN << 4 | lLSN);
                return true;
            }

            Position = lBookmark;
            rByte = 0;
            return false;
        }

        private bool ZGetHexEncodedNibble(out int rNibble)
        {
            if (Position.AtEnd) { rNibble = 0; return false; }
            if (Position.BytesLine.Literal) { rNibble = 0; return false; }

            int lByte = Position.BytesLine[Position.Byte];

            if (lByte < cASCII.ZERO) { rNibble = 0; return false; }
            if (lByte <= cASCII.NINE) { rNibble = lByte - cASCII.ZERO; ZAdvance(ref Position); return true; }

            if (lByte < cASCII.A) { rNibble = 0; return false; }
            if (lByte <= cASCII.F) { rNibble = 10 + lByte - cASCII.A; ZAdvance(ref Position); return true; }

            if (lByte < cASCII.a) { rNibble = 0; return false; }
            if (lByte <= cASCII.f) { rNibble = 10 + lByte - cASCII.a; ZAdvance(ref Position); return true; }

            rNibble = 0;
            return false;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBytesCursor));
            lBuilder.Append(mLines);
            lBuilder.Append(Position);
            return lBuilder.ToString();
        }

        public struct sPosition
        {
            public cResponseLine BytesLine;
            public int LineNumber;
            public int Byte;
            public bool AtEnd;

            public override bool Equals(object pObject) => pObject is sPosition && this == (sPosition)pObject;

            public override int GetHashCode()
            {
                unchecked
                {
                    int lHash = 17;
                    lHash = lHash * 23 + LineNumber.GetHashCode();
                    lHash = lHash * 23 + Byte.GetHashCode();
                    return lHash;
                }
            }

            public static bool operator ==(sPosition pA, sPosition pB) => ReferenceEquals(pA.BytesLine, pB.BytesLine) && pA.Byte == pB.Byte && pA.AtEnd == pB.AtEnd;
            public static bool operator !=(sPosition pA, sPosition pB) => !(pA == pB);

            public override string ToString()
            {
                if (AtEnd) return $"{nameof(sPosition)}({nameof(AtEnd)})";
                return $"{nameof(sPosition)}({LineNumber},{Byte})";
            }
        }
    }
}
