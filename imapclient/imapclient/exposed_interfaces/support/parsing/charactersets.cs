using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    public static class cASCII
    {
        // null
        public const byte NUL = 0;

        // control characters
        public const byte TAB = 9;
        public const byte LF = 10;
        public const byte CR = 13;

        // printable characters
        public const byte SPACE = 32;
        public const byte EXCLAMATION = 33;
        public const byte DQUOTE = 34;
        public const byte HASH = 35;
        public const byte PERCENT = 37;
        public const byte AMPERSAND = 38;
        public const byte QUOTE = 39;
        public const byte LPAREN = 40;
        public const byte RPAREN = 41;
        public const byte ASTERISK = 42;
        public const byte PLUS = 43;
        public const byte COMMA = 44;
        public const byte HYPEN = 45;
        public const byte DOT = 46;
        public const byte SLASH = 47;

        // 0 - 9
        public const byte ZERO = 48;
        public const byte ONE = 49;
        public const byte TWO = 50;
        public const byte THREE = 51;
        public const byte FOUR = 52;
        public const byte FIVE = 53;
        public const byte SIX = 54;
        public const byte SEVEN = 55;
        public const byte EIGHT = 56;
        public const byte NINE = 57;

        // specials

        public const byte COLON = 58;
        public const byte SEMICOLON = 59;
        public const byte LESSTHAN = 60;
        public const byte EQUALS = 61;
        public const byte GREATERTHAN = 62;
        public const byte QUESTIONMARK = 63;
        public const byte AT = 64;

        // A-Z
        public const byte A = 65;
        public const byte B = 66;
        public const byte C = 67;
        public const byte D = 68;
        public const byte E = 69;
        public const byte F = 70;
        public const byte G = 71;
        public const byte H = 72;
        public const byte I = 73;
        public const byte J = 74;
        public const byte K = 75;
        public const byte L = 76;
        public const byte M = 77;
        public const byte N = 78;
        public const byte O = 79;
        public const byte P = 80;
        public const byte Q = 81;
        public const byte R = 82;
        public const byte S = 83;
        public const byte T = 84;
        public const byte U = 85;
        public const byte V = 86;
        public const byte W = 87;
        public const byte X = 88;
        public const byte Y = 89;
        public const byte Z = 90;

        // specials

        public const byte LBRACKET = 91;
        public const byte BACKSL = 92;
        public const byte RBRACKET = 93;
        public const byte UNDERSCORE = 95;

        // a-z
        public const byte a = 97;
        public const byte b = 98;
        public const byte c = 99;
        public const byte d = 100;
        public const byte e = 101;
        public const byte f = 102;
        public const byte g = 103;
        public const byte h = 104;
        public const byte i = 105;
        public const byte j = 106;
        public const byte k = 107;
        public const byte l = 108;
        public const byte m = 109;
        public const byte n = 110;
        public const byte o = 111;
        public const byte p = 112;
        public const byte q = 113;
        public const byte r = 114;
        public const byte s = 115;
        public const byte t = 116;
        public const byte u = 117;
        public const byte v = 118;
        public const byte w = 119;
        public const byte x = 120;
        public const byte y = 121;
        public const byte z = 122;

        // specials

        public const byte LBRACE = 123;
        public const byte RBRACE = 125;
        public const byte TILDA = 126;
        public const byte DEL = 127;

        // helper functions

        public static bool Compare(byte pByte1, byte pByte2, bool pCaseSensitive)
        {
            if (pCaseSensitive) return pByte1 == pByte2;

            byte lByte1;
            if (pByte1 < a) lByte1 = pByte1;
            else if (pByte1 > z) lByte1 = pByte1;
            else lByte1 = (byte)(pByte1 - a + A);

            byte lByte2;
            if (pByte2 < a) lByte2 = pByte2;
            else if (pByte2 > z) lByte2 = pByte2;
            else lByte2 = (byte)(pByte2 - a + A);

            return lByte1 == lByte2;
        }

        public static bool Compare(IList<byte> pBytes1, IList<byte> pBytes2, bool pCaseSensitive)
        {
            if (pBytes1.Count != pBytes2.Count) return false;
            for (int i = 0; i < pBytes1.Count; i++) if (!Compare(pBytes1[i], pBytes2[i], pCaseSensitive)) return false;
            return true;
        }
    }

    public static class cChar
    {
        public const char NUL = '\0';
        public const char LF = '\n';
        public const char CR = '\r';
        public const char DEL = '\u007F';
        public const char FF = '\u00FF';
    }

    public abstract class cCharset
    {
        private const string cListWildcards = "%*";
        private const string cQuotedSpecials = "\"\\";
        private const string cAtomSpecialsSome = "(){ "; // 'atom_specials' also includes CTL, ListWildCards, QuotedSpecials and RespSpecials
        private const string cUnreservedSome = "-._~";
        private const string cACharSome = "!$'()*+,&=";
        private const string cSubDelims = "!$&'()*+,;=";

        private static readonly cBytes aListWildcards = new cBytes(cListWildcards);
        private static readonly cBytes aQuotedSpecials = new cBytes(cQuotedSpecials);
        private static readonly cBytes aAtomSpecialsSome = new cBytes(cAtomSpecialsSome);
        private static readonly cBytes aUnreservedSome = new cBytes(cUnreservedSome);
        private static readonly cBytes aACharSome = new cBytes(cACharSome);
        private static readonly cBytes aSubDelims = new cBytes(cSubDelims);

        private static bool ZIsCTL(byte pByte) => pByte < cASCII.SPACE || pByte > cASCII.TILDA;
        private static bool ZIsCTL(char pChar) => pChar < ' ' || pChar > '~';

        private static bool ZIsAlpha(byte pByte)
        {
            if (pByte < cASCII.A) return false;
            if (pByte <= cASCII.Z) return true;
            if (pByte < cASCII.a) return false;
            if (pByte > cASCII.z) return false;
            return true;
        }

        private static bool ZIsAlpha(char pChar)
        {
            if (pChar < 'A') return false;
            if (pChar <= 'Z') return true;
            if (pChar < 'a') return false;
            if (pChar > 'z') return false;
            return true;
        }

        private static bool ZIsDigit(byte pByte)
        {
            if (pByte < cASCII.ZERO) return false;
            if (pByte > cASCII.NINE) return false;
            return true;
        }

        private static bool ZIsDigit(char pChar)
        {
            if (pChar < '0') return false;
            if (pChar > '9') return false;
            return true;
        }

        private static bool ZIsUnreserved(byte pByte)
        {
            if (ZIsAlpha(pByte)) return true;
            if (ZIsDigit(pByte)) return true;
            if (ZIsOneOf(pByte, aUnreservedSome)) return true;
            return false;
        }

        private static bool ZIsUnreserved(char pChar)
        {
            if (ZIsAlpha(pChar)) return true;
            if (ZIsDigit(pChar)) return true;
            if (ZIsOneOf(pChar, cUnreservedSome)) return true;
            return false;
        }

        private static bool ZIsOneOf(byte pByte, IList<byte> pBytes)
        {
            foreach (byte lByte in pBytes) if (lByte == pByte) return true;
            return false;
        }

        private static bool ZIsOneOf(char pChar, string pChars)
        {
            foreach (byte lChar in pChars) if (lChar == pChar) return true;
            return false;
        }

        // what must be overridden

        public abstract bool Contains(byte pByte);
        public abstract bool Contains(char pChar);

        // implementations

        private class cAlpha : cCharset
        {
            public override bool Contains(byte pByte) => ZIsAlpha(pByte);
            public override bool Contains(char pChar) => ZIsAlpha(pChar);
        }

        private class cDigit : cCharset
        {
            public override bool Contains(byte pByte) => ZIsDigit(pByte);
            public override bool Contains(char pChar) => ZIsDigit(pChar);
        }

        private class cAlphaNumeric : cCharset
        {
            public override bool Contains(byte pByte) => ZIsAlpha(pByte) || ZIsDigit(pByte);
            public override bool Contains(char pChar) => ZIsAlpha(pChar) || ZIsDigit(pChar);
        }

        private class cScheme : cCharset
        {
            private const string cSchemeSome = "+-.";
            private static readonly cBytes aSchemeSome = new cBytes(cSchemeSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, aSchemeSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, cSchemeSome)) return true;
                return false;
            }
        }

        private class cUserInfo : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aSubDelims)) return true;
                if (pByte == cASCII.COLON) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cSubDelims)) return true;
                if (pChar == ':') return true;
                return false;
            }
        }

        private class cAtom : cCharset
        {
            private const string cRespSpecials = "]";
            private static readonly cBytes aRespSpecials = new cBytes(cRespSpecials);

            public override bool Contains(byte pByte)
            {
                if (ZIsCTL(pByte)) return false;
                if (ZIsOneOf(pByte, aAtomSpecialsSome)) return false;
                if (ZIsOneOf(pByte, aListWildcards)) return false;
                if (ZIsOneOf(pByte, aQuotedSpecials)) return false;
                if (ZIsOneOf(pByte, aRespSpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsCTL(pChar)) return false;
                if (ZIsOneOf(pChar, cAtomSpecialsSome)) return false;
                if (ZIsOneOf(pChar, cListWildcards)) return false;
                if (ZIsOneOf(pChar, cQuotedSpecials)) return false;
                if (ZIsOneOf(pChar, cRespSpecials)) return false;
                return true;
            }
        }

        private class cAString : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsCTL(pByte)) return false;
                if (ZIsOneOf(pByte, aAtomSpecialsSome)) return false;
                if (ZIsOneOf(pByte, aListWildcards)) return false;
                if (ZIsOneOf(pByte, aQuotedSpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsCTL(pChar)) return false;
                if (ZIsOneOf(pChar, cAtomSpecialsSome)) return false;
                if (ZIsOneOf(pChar, cListWildcards)) return false;
                if (ZIsOneOf(pChar, cQuotedSpecials)) return false;
                return true;
            }
        }

        private class cTextNotRBRACKET : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (pByte < cASCII.SPACE || pByte > cASCII.TILDA) return false;
                if (pByte == cASCII.RBRACKET) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar < ' ' || pChar > '~') return false;
                if (pChar == ']') return false;
                return true;
            }
        }

        private class cListMailbox : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsCTL(pByte)) return false;
                if (ZIsOneOf(pByte, aAtomSpecialsSome)) return false;
                if (ZIsOneOf(pByte, aQuotedSpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsCTL(pChar)) return false;
                if (ZIsOneOf(pChar, cAtomSpecialsSome)) return false;
                if (ZIsOneOf(pChar, cQuotedSpecials)) return false;
                return true;
            }
        }

        private class cAChar : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aACharSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cACharSome)) return true;
                return false;
            }
        }

        private class cBChar : cCharset
        {
            private const string cBCharSome = ":@/";
            private static readonly cBytes aBCharSome = new cBytes(cBCharSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aACharSome)) return true;
                if (ZIsOneOf(pByte, aBCharSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cACharSome)) return true;
                if (ZIsOneOf(pChar, cBCharSome)) return true;
                return false;
            }
        }

        private class cPathSegment : cCharset
        {
            private const string cPathSegmentSome = ":@";
            private static readonly cBytes aPathSegmentSome = new cBytes(cPathSegmentSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aSubDelims)) return true;
                if (ZIsOneOf(pByte, aPathSegmentSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cSubDelims)) return true;
                if (ZIsOneOf(pChar, cPathSegmentSome)) return true;
                return false;
            }
        }

        private class cPathSegmentNoColon : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aSubDelims)) return true;
                if (pByte == cASCII.AT) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cSubDelims)) return true;
                if (pChar == '@') return true;
                return false;
            }
        }

        private class cPath : cCharset
        {
            private const string cPathSome = ":@/";
            private static readonly cBytes aPathSome = new cBytes(cPathSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aSubDelims)) return true;
                if (ZIsOneOf(pByte, aPathSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cSubDelims)) return true;
                if (ZIsOneOf(pChar, cPathSome)) return true;
                return false;
            }
        }

        private class cAfterPath : cCharset
        {
            private const string cAfterPathSome = ":@/?";
            private static readonly cBytes aAfterPathSome = new cBytes(cAfterPathSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aSubDelims)) return true;
                if (ZIsOneOf(pByte, aAfterPathSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cSubDelims)) return true;
                if (ZIsOneOf(pChar, cAfterPathSome)) return true;
                return false;
            }
        }

        private class cRegName : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aSubDelims)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cSubDelims)) return true;
                return false;
            }
        }

        private class cIPLiteral : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, aSubDelims)) return true;
                if (pByte == cASCII.COLON) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, cSubDelims)) return true;
                if (pChar == ':') return true;
                return false;
            }
        }

        private class cUAuthMechanism : cCharset
        {
            private const string cUAuthMechanismSome = "-.";
            private static readonly cBytes aUAuthMechanismSome = new cBytes(cUAuthMechanismSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, aUAuthMechanismSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, cUAuthMechanismSome)) return true;
                return false;
            }
        }

        private class cHexidecimal : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsDigit(pByte)) return true;
                if (pByte < cASCII.A) return false;
                if (pByte <= cASCII.F) return true;
                if (pByte < cASCII.a) return false;
                if (pByte > cASCII.f) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsDigit(pChar)) return true;
                if (pChar < 'A') return false;
                if (pChar <= 'F') return true;
                if (pChar < 'a') return false;
                if (pChar > 'f') return false;
                return true;
            }
        }

        private class cCharsetName : cCharset
        {
            // rfc 2978

            private const string cCharsetNameSome = "!#$%&'+-^_`{}~";
            private static readonly cBytes aCharsetNameSome = new cBytes(cCharsetNameSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, aCharsetNameSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, cCharsetNameSome)) return true;
                return false;
            }
        }

        private class cAll : cCharset
        {
            public override bool Contains(byte pByte) => true;
            public override bool Contains(char pChar) => true;
        }

        private class cBase64 : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (pByte == cASCII.PLUS || pByte == cASCII.SLASH || pByte == cASCII.EQUALS) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (pChar == '+' || pChar == '/' || pChar == '=') return true;
                return false;
            }
        }

        private class cQEncoding : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (pByte < cASCII.EXCLAMATION) return false;
                if (pByte > cASCII.TILDA) return false;
                if (pByte == cASCII.EQUALS) return false;
                if (pByte == cASCII.QUESTIONMARK) return false;
                if (pByte == cASCII.UNDERSCORE) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar < '!') return false;
                if (pChar > '~') return false;
                if (pChar == '=') return false;
                if (pChar == '?') return false;
                if (pChar == '_') return false;
                return true;
            }
        }

        private class cRFC822HeaderField : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsCTL(pByte)) return false;
                if (pByte == cASCII.SPACE) return false;
                if (pByte == cASCII.COLON) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsCTL(pChar)) return false;
                if (pChar == ' ') return false;
                if (pChar == ':') return false;
                return true;
            }
        }

        // instances

        public static readonly cCharset Alpha = new cAlpha();
        public static readonly cCharset Digit = new cDigit();
        public static readonly cCharset AlphaNumeric = new cAlphaNumeric();
        public static readonly cCharset Scheme = new cScheme();
        public static readonly cCharset UserInfo = new cUserInfo();
        public static readonly cCharset Atom = new cAtom();
        public static readonly cCharset AString = new cAString();
        public static readonly cCharset TextNotRBRACKET = new cTextNotRBRACKET();
        public static readonly cCharset ListMailbox = new cListMailbox();
        public static readonly cCharset AChar = new cAChar();
        public static readonly cCharset BChar = new cBChar();
        public static readonly cCharset PathSegment = new cPathSegment();
        public static readonly cCharset PathSegmentNoColon = new cPathSegmentNoColon();
        public static readonly cCharset Path = new cPath();
        public static readonly cCharset AfterPath = new cAfterPath();
        public static readonly cCharset RegName = new cRegName();
        public static readonly cCharset IPLiteral = new cIPLiteral();
        public static readonly cCharset UAuthMechanism = new cUAuthMechanism();
        public static readonly cCharset Hexidecimal = new cHexidecimal();
        public static readonly cCharset CharsetName = new cCharsetName();
        public static readonly cCharset All = new cAll();
        public static readonly cCharset Base64 = new cBase64();
        public static readonly cCharset QEncoding = new cQEncoding();
        public static readonly cCharset RFC822HeaderField = new cRFC822HeaderField();
    }

    public static class cASCIIMonth
    {
        public static readonly cBytes Jan = new cBytes("JAN");
        public static readonly cBytes Feb = new cBytes("FEB");
        public static readonly cBytes Mar = new cBytes("MAR");
        public static readonly cBytes Apr = new cBytes("APR");
        public static readonly cBytes May = new cBytes("MAY");
        public static readonly cBytes Jun = new cBytes("JUN");
        public static readonly cBytes Jul = new cBytes("JUL");
        public static readonly cBytes Aug = new cBytes("AUG");
        public static readonly cBytes Sep = new cBytes("SEP");
        public static readonly cBytes Oct = new cBytes("OCT");
        public static readonly cBytes Nov = new cBytes("NOV");
        public static readonly cBytes Dec = new cBytes("DEC");

        public static readonly ReadOnlyCollection<cBytes> Name = Array.AsReadOnly(new cBytes[] { Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec });
    }
}