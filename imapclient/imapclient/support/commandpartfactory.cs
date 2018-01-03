﻿using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal enum eURLMailboxEncoding { utf7, utf8 }

    internal class cCommandPartFactory
    {
        public static readonly cCommandPartFactory Validation = new cCommandPartFactory(false, null, eURLMailboxEncoding.utf8);

        private static readonly cBytes UTC = new cBytes("+0000");

        public readonly bool UTF8Enabled;
        public readonly Encoding Encoding;
        public readonly eURLMailboxEncoding URLMailboxEncoding;
        public readonly cTextCommandPart CharsetName;

        public cCommandPartFactory(bool pUTF8Enabled, Encoding pEncoding, eURLMailboxEncoding pURLMailboxEncoding)
        {
            UTF8Enabled = pUTF8Enabled;

            if (pUTF8Enabled) CharsetName = null;
            else
            {
                Encoding = pEncoding;
                if (pEncoding == null) CharsetName = null;
                else if (!TryAsCharsetName(pEncoding.WebName, out CharsetName)) throw new ArgumentOutOfRangeException(nameof(pEncoding));
            }

            URLMailboxEncoding = pURLMailboxEncoding;
        }

        public bool TryAsString(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            bool lResult;
            cTextCommandPart lText;
            cLiteralCommandPart lLiteral;

            if (ZTryAsQuotedASCII(pString, pSecret, out lText)) { rResult = lText; return true; }
            if (TryAsASCIILiteral(pString, pSecret, out lLiteral)) { rResult = lLiteral; return true; }

            if (UTF8Enabled)
            {
                lResult = ZTryAsQuotedUTF8(pString, pSecret, out lText);
                rResult = lText;
                return lResult;
            }

            if (Encoding == null) { rResult = null; return false; }

            var lBytes = Encoding.GetBytes(pString);

            if (ZTryAsQuotedASCII(lBytes, pSecret, true, out lText)) { rResult = lText; return true; }

            lResult = ZTryAsLiteral(lBytes, pSecret, true, out lLiteral);
            rResult = lLiteral;
            return lResult;
        }

        public cCommandPart AsString(string pString, bool pSecret = false)
        {
            if (TryAsString(pString, pSecret, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public bool TryAsAString(string pString, bool pSecret, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            bool lResult;
            cTextCommandPart lText;
            cLiteralCommandPart lLiteral;

            if (ZTryAsBytesInCharset(pString, cCharset.AString, pSecret, out lText)) { rResult = lText; return true; }
            if (ZTryAsQuotedASCII(pString, pSecret, out lText)) { rResult = lText; return true; }
            if (TryAsASCIILiteral(pString, pSecret, out lLiteral)) { rResult = lLiteral; return true; }

            if (UTF8Enabled)
            {
                lResult = ZTryAsQuotedUTF8(pString, pSecret, out lText);
                rResult = lText;
                return lResult;
            }

            if (Encoding == null) { rResult = null; return false; }

            var lBytes = Encoding.GetBytes(pString);

            if (ZTryAsBytesInCharset(lBytes, cCharset.AString, pSecret, true, out lText)) { rResult = lText; return true; }
            if (ZTryAsQuotedASCII(lBytes, pSecret, true, out lText)) { rResult = lText; return true; }

            lResult = ZTryAsLiteral(lBytes, pSecret, true, out lLiteral);
            rResult = lLiteral;
            return lResult;
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

        public bool TryAsMailbox(string pMailboxPath, char? pDelimiter, out cCommandPart rCommandPart, out string rEncodedMailboxPath)
        {
            if (pMailboxPath == null) { rCommandPart = null; rEncodedMailboxPath = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rCommandPart = null; rEncodedMailboxPath = null; return false; }
            return ZTryAsMailbox(pMailboxPath, pDelimiter, cCharset.AString, out rCommandPart, out rEncodedMailboxPath);
        }

        private bool ZTryAsMailbox(string pString, char? pDelimiter, cCharset pCharset, out cCommandPart rCommandPart, out string rEncodedMailboxPath)
        {
            if (pString.Equals(cMailboxName.InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                rCommandPart = cCommandPart.Inbox;
                rEncodedMailboxPath = cMailboxName.InboxString;
                return true;
            }

            cTextCommandPart lText;
            cLiteralCommandPart lLiteral;

            if (ZTryAsBytesInCharset(pString, pCharset, false, out lText))
            {
                rCommandPart = lText;
                rEncodedMailboxPath = pString;
                return true;
            }

            if (UTF8Enabled)
            {
                if (ZTryAsQuotedUTF8(pString, false, out lText))
                {
                    rCommandPart = lText;
                    rEncodedMailboxPath = pString;
                    return true;
                }

                rCommandPart = null;
                rEncodedMailboxPath = null;
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

            if (ZTryAsBytesInCharset(lBytes, pCharset, false, false, out lText) || ZTryAsQuotedASCII(lBytes, false, false, out lText))
            {
                rCommandPart = lText;
                rEncodedMailboxPath = cTools.ASCIIBytesToString(lBytes);
                return true;
            }

            if (ZTryAsASCIILiteral(lBytes, false, out lLiteral))
            {
                rCommandPart = lLiteral;
                rEncodedMailboxPath = cTools.ASCIIBytesToString(lBytes);
                return true;
            }

            rCommandPart = null;
            rEncodedMailboxPath = null;
            return false;
        }

        public bool TryAsURLMailbox(string pMailboxPath, char? pDelimiter, out string rEncodedMailboxPath)
        {
            // should the transmitted value include the quotes? the RFC seems clear - mailbox does include quotes if they are required
            //  HOWEVER: then you have to ask: should it include the literal header (the {length}) and then, should it include the binary, literal+/- etc?
            //  at this stage then I've assumed then that it should include the content only and none of the decoration that is required to get it over the wire 
            //  BUT: this still leaves the UTF7 conversion, which I had assumed would be required
            //  however it seems that Dovecot at least doesn't like the UTF7 encoded mailbox name. At this stage I don't know if this is "standard" behaviour or not

            if (pMailboxPath == null) { rEncodedMailboxPath = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rEncodedMailboxPath = null; return false; }

            switch (URLMailboxEncoding)
            {
                case eURLMailboxEncoding.utf7:

                    return ZTryAsMailbox(pMailboxPath, pDelimiter, cCharset.AString, out _, out rEncodedMailboxPath);

                case eURLMailboxEncoding.utf8:

                    if (pMailboxPath.Equals(cMailboxName.InboxString, StringComparison.InvariantCultureIgnoreCase))
                    {
                        rEncodedMailboxPath = cMailboxName.InboxString;
                        return true;
                    }

                    if (ZTryAsBytesInCharset(pMailboxPath, cCharset.AString, false, out _) || ZTryAsQuotedUTF8(pMailboxPath, false, out _))
                    {
                        rEncodedMailboxPath = pMailboxPath;
                        return true;
                    }

                    rEncodedMailboxPath = null;
                    return false;

                default:

                    throw new cInternalErrorException();
            }
        }

        public static cTextCommandPart AsDate(DateTime pDate)
        {
            var lBytes = new cByteList(11);

            lBytes.AddRange(ZIntToBytes(pDate.Day, 0));
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(cASCIIMonth.Name[pDate.Month - 1]);
            lBytes.Add(cASCII.HYPEN);
            lBytes.AddRange(ZIntToBytes(pDate.Year, 4));

            return new cTextCommandPart(lBytes);
        }

        public static cTextCommandPart AsDateTime(DateTime pDate)
        {
            var lBytes = new cByteList(27);

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

            lBytes.Add(cASCII.DQUOTE);

            return new cTextCommandPart(lBytes);
        }

        public static bool TryAsAtom(string pString, out cTextCommandPart rResult) => ZTryAsBytesInCharset(pString, cCharset.Atom, false, out rResult);

        public static cCommandPart AsAtom(string pString)
        {
            if (ZTryAsBytesInCharset(pString, cCharset.Atom, false, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public static bool TryAsASCIILiteral(string pString, bool pSecret, out cLiteralCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (lChar == cChar.NUL || lChar > cChar.DEL) { rResult = null; return false; }
                lBytes.Add((byte)lChar);
            }

            rResult = new cLiteralCommandPart(lBytes, false, pSecret);
            return true;
        }

        public static cCommandPart AsASCIILiteral(string pString, bool pSecret = false)
        {
            if (TryAsASCIILiteral(pString, pSecret, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public static bool TryAsASCIIString(string pString, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }
            if (ZTryAsQuotedASCII(pString, false, out var lText)) { rResult = lText; return true; }
            if (TryAsASCIILiteral(pString, false, out var lLiteral)) { rResult = lLiteral; return true; }
            rResult = null;
            return false;
        }

        public static cCommandPart AsASCIIString(string pString)
        {
            if (TryAsASCIIString(pString, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public static bool TryAsUTF8String(string pString, out cTextCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }
            return ZTryAsQuotedUTF8(pString, false, out rResult);
        }

        public static bool TryAsASCIIAString(string pString, out cCommandPart rResult)
        {
            if (pString == null) { rResult = null; return false; }

            cTextCommandPart lText;
            if (ZTryAsBytesInCharset(pString, cCharset.AString, false, out lText)) { rResult = lText; return true; }
            if (ZTryAsQuotedASCII(pString, false, out lText)) { rResult = lText; return true; }

            if (TryAsASCIILiteral(pString, false, out var lLiteral)) { rResult = lLiteral; return true; }

            rResult = null;
            return false;
        }

        public static cCommandPart AsASCIIAString(string pString)
        {
            if (TryAsASCIIAString(pString, out var lResult)) return lResult;
            throw new ArgumentOutOfRangeException(nameof(pString));
        }

        public static bool TryAsCharsetName(string pString, out cTextCommandPart rResult)
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

        private static bool ZTryAsBytesInCharset(string pString, cCharset pCharset, bool pSecret, out cTextCommandPart rResult)
        {
            if (string.IsNullOrEmpty(pString)) { rResult = null; return false; }

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (!pCharset.Contains(lChar)) { rResult = null; return false; }
                lBytes.Add((byte)lChar);
            }

            rResult = new cTextCommandPart(lBytes, pSecret);
            return true;
        }

        private static bool ZTryAsBytesInCharset(IList<byte> pBytes, cCharset pCharset, bool pSecret, bool pEncoded, out cTextCommandPart rResult)
        {
            if (pBytes == null || pBytes.Count == 0) { rResult = null; return false; }
            foreach (byte lByte in pBytes) if (!pCharset.Contains(lByte)) { rResult = null; return false; }
            rResult = new cTextCommandPart(pBytes, pSecret, pEncoded);
            return true;
        }

        private static bool ZTryAsASCIILiteral(IList<byte> pBytes, bool pSecret, out cLiteralCommandPart rResult)
        {
            if (pBytes == null) { rResult = null; return false; }
            foreach (byte lByte in pBytes) if (lByte == cASCII.NUL || lByte > cASCII.DEL) { rResult = null; return false; }
            rResult = new cLiteralCommandPart(pBytes, false, pSecret);
            return true;
        }

        private static bool ZTryAsLiteral(IList<byte> pBytes, bool pSecret, bool pEncoded, out cLiteralCommandPart rResult)
        {
            if (pBytes == null) { rResult = null; return false; }
            foreach (byte lByte in pBytes) if (lByte == cASCII.NUL) { rResult = null; return false; }
            rResult = new cLiteralCommandPart(pBytes, false, pSecret, pEncoded);
            return true;
        }

        private static bool ZTryAsQuotedASCII(string pString, bool pSecret, out cTextCommandPart rResult)
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

            rResult = new cTextCommandPart(lBytes, pSecret);
            return true;
        }

        private static bool ZTryAsQuotedASCII(IList<byte> pBytes, bool pSecret, bool pEncoded, out cTextCommandPart rResult)
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

            rResult = new cTextCommandPart(lBytes, pSecret, pEncoded);
            return true;
        }

        private static bool ZTryAsQuotedUTF8(string pString, bool pSecret, out cTextCommandPart rResult)
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

            rResult = new cTextCommandPart(lBytes, pSecret);
            return true;
        }
    }
}