using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents a set of characters that are valid in a parsing context. Intended for internal use.
    /// </summary>
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

        /// <summary>
        /// Determines whether the specified byte is contained in the set of characters.
        /// </summary>
        /// <param name="pByte"></param>
        /// <returns></returns>
        public abstract bool Contains(byte pByte);

        /// <summary>
        /// Determines whether the specified char is contained in the set of characters.
        /// </summary>
        /// <param name="pChar"></param>
        /// <returns></returns>
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

        private class cCharsetNameDash : cCharset
        {
            // rfc 2978 modified for rfc 2231

            private const string cCharsetNameSome = "!#$%&+-^_`{}~"; // note: excludes "'" which is actually a legal character in a charset name
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

        private class cCText : cCharset
        {
            private const string cCTextDisallowed = "\0\t\n\r ()\\";
            private static readonly cBytes aCTextDisallowed = new cBytes(cCTextDisallowed);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsOneOf(pByte, aCTextDisallowed)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsOneOf(pChar, cCTextDisallowed)) return false;
                return true;
            }
        }

        private class cAText : cCharset
        {
            private const string cATextSome = "!#$%&'*+-/=?^_`{|}~";
            private static readonly cBytes aATextSome = new cBytes(cATextSome);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, aATextSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, cATextSome)) return true;
                return false;
            }
        }

        private class cQText : cCharset
        {
            private const string cQTextDisallowed = "\0\t\n\r \"\\";
            private static readonly cBytes aQTextDisallowed = new cBytes(cQTextDisallowed);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsOneOf(pByte, aQTextDisallowed)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsOneOf(pChar, cQTextDisallowed)) return false;
                return true;
            }
        }

        private class cDText : cCharset
        {
            private const string cDTextDisallowed = "\0\t\n\r [\\]";
            private static readonly cBytes aDTextDisallowed = new cBytes(cDTextDisallowed);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsOneOf(pByte, aDTextDisallowed)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsOneOf(pChar, cDTextDisallowed)) return false;
                return true;
            }
        }

        private class cFText : cCharset
        {
            public override bool Contains(byte pByte) => pByte > cASCII.SPACE && pByte != cASCII.COLON && pByte < cASCII.DEL;
            public override bool Contains(char pChar) => pChar > ' ' && pChar != ':' && pChar < cChar.DEL;
        }

        private class cVSChar : cCharset
        {
            public override bool Contains(byte pByte) => pByte >= cASCII.SPACE && pByte < cASCII.DEL;
            public override bool Contains(char pChar) => pChar >= ' ' && pChar < cChar.DEL;
        }

        // instances

        /**<summary>Represents the characters A-Z and a-z.</summary>*/
        public static readonly cCharset Alpha = new cAlpha();
        /**<summary>Represents the characters 0-9.</summary>*/
        public static readonly cCharset Digit = new cDigit();
        /**<summary>Represents the characters A-Z, 0-9 and a-z.</summary>*/
        public static readonly cCharset AlphaNumeric = new cAlphaNumeric();
        /**<summary>Represents the characters used in RFC 3986 'scheme'.</summary>*/
        public static readonly cCharset Scheme = new cScheme();
        /**<summary>Represents the characters used in RFC 3986 'userinfo'.</summary>*/
        public static readonly cCharset UserInfo = new cUserInfo();
        /**<summary>Represents the characters used in RFC 3501 'atom'.</summary>*/
        public static readonly cCharset Atom = new cAtom();
        /**<summary>Represents the characters used in RFC 3501 'astring'.</summary>*/
        public static readonly cCharset AString = new cAString();
        /**<summary>Represents the characters used in RFC 3501 response text.</summary>*/
        public static readonly cCharset TextNotRBRACKET = new cTextNotRBRACKET();
        /**<summary>Represents the characters used in RFC 3501 'list-mailbox'.</summary>*/
        public static readonly cCharset ListMailbox = new cListMailbox();
        /**<summary>Represents the characters used in RFC 5092 'achar'.</summary>*/
        public static readonly cCharset AChar = new cAChar();
        /**<summary>Represents the characters used in RFC 5092 'bchar'.</summary>*/
        public static readonly cCharset BChar = new cBChar();
        /**<summary>Represents the characters used in RFC 3986 'segment'.</summary>*/
        public static readonly cCharset PathSegment = new cPathSegment();
        /**<summary>Represents the characters used in RFC 3986 'segment-nz-nc'.</summary>*/
        public static readonly cCharset PathSegmentNoColon = new cPathSegmentNoColon();
        /**<summary>Represents the characters used in various RFC 3986 path components (= PathSegment + '/').</summary>*/
        public static readonly cCharset Path = new cPath();
        /**<summary>Represents the characters used in RFC 3986 'query' and 'fragment'.</summary>*/
        public static readonly cCharset AfterPath = new cAfterPath();
        /**<summary>Represents the characters used in RFC 3986 'reg-name'.</summary>*/
        public static readonly cCharset RegName = new cRegName();
        /**<summary>Represents the characters used in the RFC 3986 'IP-literal'.</summary>*/
        public static readonly cCharset IPLiteral = new cIPLiteral();
        /**<summary>Represents the characters used in RFC 5092 'uauth-mechanism'.</summary>*/
        public static readonly cCharset UAuthMechanism = new cUAuthMechanism();
        /**<summary>Represents the characters A-F, 0-9 and a-f.</summary>*/
        public static readonly cCharset Hexidecimal = new cHexidecimal();
        /**<summary>Represents the characters used in RFC 2978 'mime-charset-chars'.</summary>*/
        public static readonly cCharset CharsetName = new cCharsetName();
        /**<summary>Represents the characters used in RFC 2231 'charset'.</summary>*/
        public static readonly cCharset CharsetNameDash = new cCharsetNameDash();
        /**<summary>Represents a character set that contains all characters.</summary>*/
        public static readonly cCharset All = new cAll();
        /**<summary>Represents the characters A-Z, 0-9, a-z, +/=.</summary>*/
        public static readonly cCharset Base64 = new cBase64();
        /**<summary>Represents the characters used in RFC 2047 Quoted-Printable.</summary>*/
        public static readonly cCharset QEncoding = new cQEncoding();
        /**<summary>Represents the characters used in RFC 6532 'ctext'.</summary>*/
        public static readonly cCharset CText = new cCText();
        /**<summary>Represents the characters used in RFC 6532 'atext'.</summary>*/
        public static readonly cCharset AText = new cAText();
        /**<summary>Represents the characters used in RFC 6532 'qtext'.</summary>*/
        public static readonly cCharset QText = new cQText();
        /**<summary>Represents the characters used in RFC 6532 'dtext'.</summary>*/
        public static readonly cCharset DText = new cDText();
        /**<summary>Represents the characters used in RFC 5322 'ftext'.</summary>*/
        public static readonly cCharset FText = new cFText();
        /**<summary>Represents the characters used in RFC 6749 'VSCHAR'.</summary>*/
        public static readonly cCharset VSChar = new cVSChar();
    }
}