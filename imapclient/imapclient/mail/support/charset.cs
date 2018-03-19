using System;
using System.Collections.Generic;

namespace work.bacome.mailclient.support
{
    /// <summary>
    /// Represents a set of characters that are valid in a parsing context. Intended for internal use.
    /// </summary>
    public abstract class cCharset
    {
        internal cCharset() { }

        private const string kcListWildcards = "%*";
        private const string kcQuotedSpecials = "\"\\";
        private const string kcAtomSpecialsSome = "(){ "; // 'atom_specials' also includes CTL, ListWildCards, QuotedSpecials and RespSpecials
        private const string kcUnreservedSome = "-._~";
        private const string kcACharSome = "!$'()*+,&=";
        private const string kcSubDelims = "!$&'()*+,;=";
        private const string kcTSpecials = "()<>@,;:\\\"/[]?=";

        private static readonly cBytes kaListWildcards = new cBytes(kcListWildcards);
        private static readonly cBytes kaQuotedSpecials = new cBytes(kcQuotedSpecials);
        private static readonly cBytes kaAtomSpecialsSome = new cBytes(kcAtomSpecialsSome);
        private static readonly cBytes kaUnreservedSome = new cBytes(kcUnreservedSome);
        private static readonly cBytes kaACharSome = new cBytes(kcACharSome);
        private static readonly cBytes kaSubDelims = new cBytes(kcSubDelims);
        private static readonly cBytes kaTSpecials = new cBytes(kcTSpecials);

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
            if (ZIsOneOf(pByte, kaUnreservedSome)) return true;
            return false;
        }

        private static bool ZIsUnreserved(char pChar)
        {
            if (ZIsAlpha(pChar)) return true;
            if (ZIsDigit(pChar)) return true;
            if (ZIsOneOf(pChar, kcUnreservedSome)) return true;
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


        public bool ContainsAll(IEnumerable<char> pChars)
        {
            foreach (char lChar in pChars) if (!Contains(lChar)) return false;
            return true;
        }

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
            private const string kcSchemeSome = "+-.";
            private static readonly cBytes kaSchemeSome = new cBytes(kcSchemeSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, kaSchemeSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, kcSchemeSome)) return true;
                return false;
            }
        }

        private class cUserInfo : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaSubDelims)) return true;
                if (pByte == cASCII.COLON) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcSubDelims)) return true;
                if (pChar == ':') return true;
                return false;
            }
        }

        private class cAtom : cCharset
        {
            private const string kcRespSpecials = "]";
            private static readonly cBytes kaRespSpecials = new cBytes(kcRespSpecials);

            public override bool Contains(byte pByte)
            {
                if (ZIsCTL(pByte)) return false;
                if (ZIsOneOf(pByte, kaAtomSpecialsSome)) return false;
                if (ZIsOneOf(pByte, kaListWildcards)) return false;
                if (ZIsOneOf(pByte, kaQuotedSpecials)) return false;
                if (ZIsOneOf(pByte, kaRespSpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsCTL(pChar)) return false;
                if (ZIsOneOf(pChar, kcAtomSpecialsSome)) return false;
                if (ZIsOneOf(pChar, kcListWildcards)) return false;
                if (ZIsOneOf(pChar, kcQuotedSpecials)) return false;
                if (ZIsOneOf(pChar, kcRespSpecials)) return false;
                return true;
            }
        }

        private class cAString : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsCTL(pByte)) return false;
                if (ZIsOneOf(pByte, kaAtomSpecialsSome)) return false;
                if (ZIsOneOf(pByte, kaListWildcards)) return false;
                if (ZIsOneOf(pByte, kaQuotedSpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsCTL(pChar)) return false;
                if (ZIsOneOf(pChar, kcAtomSpecialsSome)) return false;
                if (ZIsOneOf(pChar, kcListWildcards)) return false;
                if (ZIsOneOf(pChar, kcQuotedSpecials)) return false;
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
                if (ZIsOneOf(pByte, kaAtomSpecialsSome)) return false;
                if (ZIsOneOf(pByte, kaQuotedSpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsCTL(pChar)) return false;
                if (ZIsOneOf(pChar, kcAtomSpecialsSome)) return false;
                if (ZIsOneOf(pChar, kcQuotedSpecials)) return false;
                return true;
            }
        }

        private class cAChar : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaACharSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcACharSome)) return true;
                return false;
            }
        }

        private class cBChar : cCharset
        {
            private const string kcBCharSome = ":@/";
            private static readonly cBytes kaBCharSome = new cBytes(kcBCharSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaACharSome)) return true;
                if (ZIsOneOf(pByte, kaBCharSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcACharSome)) return true;
                if (ZIsOneOf(pChar, kcBCharSome)) return true;
                return false;
            }
        }

        private class cPathSegment : cCharset
        {
            private const string kcPathSegmentSome = ":@";
            private static readonly cBytes kaPathSegmentSome = new cBytes(kcPathSegmentSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaSubDelims)) return true;
                if (ZIsOneOf(pByte, kaPathSegmentSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcSubDelims)) return true;
                if (ZIsOneOf(pChar, kcPathSegmentSome)) return true;
                return false;
            }
        }

        private class cPathSegmentNoColon : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaSubDelims)) return true;
                if (pByte == cASCII.AT) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcSubDelims)) return true;
                if (pChar == '@') return true;
                return false;
            }
        }

        private class cPath : cCharset
        {
            private const string kcPathSome = ":@/";
            private static readonly cBytes kaPathSome = new cBytes(kcPathSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaSubDelims)) return true;
                if (ZIsOneOf(pByte, kaPathSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcSubDelims)) return true;
                if (ZIsOneOf(pChar, kcPathSome)) return true;
                return false;
            }
        }

        private class cAfterPath : cCharset
        {
            private const string kcAfterPathSome = ":@/?";
            private static readonly cBytes kaAfterPathSome = new cBytes(kcAfterPathSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaSubDelims)) return true;
                if (ZIsOneOf(pByte, kaAfterPathSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcSubDelims)) return true;
                if (ZIsOneOf(pChar, kcAfterPathSome)) return true;
                return false;
            }
        }

        private class cRegName : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaSubDelims)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcSubDelims)) return true;
                return false;
            }
        }

        private class cIPLiteral : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (ZIsUnreserved(pByte)) return true;
                if (ZIsOneOf(pByte, kaSubDelims)) return true;
                if (pByte == cASCII.COLON) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsUnreserved(pChar)) return true;
                if (ZIsOneOf(pChar, kcSubDelims)) return true;
                if (pChar == ':') return true;
                return false;
            }
        }

        private class cUAuthMechanism : cCharset
        {
            private const string kcUAuthMechanismSome = "-.";
            private static readonly cBytes kaUAuthMechanismSome = new cBytes(kcUAuthMechanismSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, kaUAuthMechanismSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, kcUAuthMechanismSome)) return true;
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

            private const string kcCharsetNameSome = "!#$%&'+-^_`{}~";
            private static readonly cBytes kaCharsetNameSome = new cBytes(kcCharsetNameSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, kaCharsetNameSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, kcCharsetNameSome)) return true;
                return false;
            }
        }

        private class cCharsetNameDash : cCharset
        {
            // rfc 2978 modified for rfc 2231

            private const string kcCharsetNameSome = "!#$%&+-^_`{}~"; // note: excludes "'" which is actually a legal character in a charset name
            private static readonly cBytes kaCharsetNameSome = new cBytes(kcCharsetNameSome);

            public override bool Contains(byte pByte)
            {
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, kaCharsetNameSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, kcCharsetNameSome)) return true;
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

        private class cObsCText : cCharset
        {
            private const string kcCTextDisallowed = "\0\t\n\r ()\\";
            private static readonly cBytes kaCTextDisallowed = new cBytes(kcCTextDisallowed);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsOneOf(pByte, kaCTextDisallowed)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsOneOf(pChar, kcCTextDisallowed)) return false;
                return true;
            }
        }

        private class cAText : cCharset
        {
            private const string kcATextSome = "!#$%&'*+-/=?^_`{|}~";
            private static readonly cBytes kaATextSome = new cBytes(kcATextSome);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsAlpha(pByte)) return true;
                if (ZIsDigit(pByte)) return true;
                if (ZIsOneOf(pByte, kaATextSome)) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsAlpha(pChar)) return true;
                if (ZIsDigit(pChar)) return true;
                if (ZIsOneOf(pChar, kcATextSome)) return true;
                return false;
            }
        }

        private class cObsQText : cCharset
        {
            private const string kcQTextDisallowed = "\0\t\n\r \"\\";
            private static readonly cBytes kaQTextDisallowed = new cBytes(kcQTextDisallowed);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsOneOf(pByte, kaQTextDisallowed)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsOneOf(pChar, kcQTextDisallowed)) return false;
                return true;
            }
        }

        private class cDText : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (pByte <= cASCII.SPACE || pByte == cASCII.LBRACKET || pByte == cASCII.BACKSL || pByte == cASCII.RBRACKET || pByte == cASCII.DEL) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (pChar <= ' ' || pChar == '[' || pChar == '\\' || pChar == ']' || pChar == cChar.DEL) return false;
                return true;
            }
        }

        private class cObsDText : cCharset
        {
            private const string kcDTextDisallowed = "\0\t\n\r [\\]";
            private static readonly cBytes kaDTextDisallowed = new cBytes(kcDTextDisallowed);

            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (ZIsOneOf(pByte, kaDTextDisallowed)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (ZIsOneOf(pChar, kcDTextDisallowed)) return false;
                return true;
            }
        }

        private class cFText : cCharset
        {
            public override bool Contains(byte pByte) => pByte > cASCII.SPACE && pByte != cASCII.COLON && pByte < cASCII.DEL;
            public override bool Contains(char pChar) => pChar > ' ' && pChar != ':' && pChar < cChar.DEL;
        }

        private class cWSPVChar : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (pByte > cASCII.DEL) return true; // allows utf8 to pass through (unvalidated)
                if (pByte == cASCII.TAB) return true;
                if (pByte < cASCII.SPACE) return false;
                if (pByte < cASCII.DEL) return true;
                return false;
            }

            public override bool Contains(char pChar)
            {
                if (pChar > cChar.DEL) return true; // allows utf16 to pass through (unvalidated)
                if (pChar == '\t') return true;
                if (pChar < ' ') return false;
                if (pChar < cChar.DEL) return true;
                return false;
            }
        }

        private class cVSChar : cCharset
        {
            public override bool Contains(byte pByte) => pByte >= cASCII.SPACE && pByte < cASCII.DEL;
            public override bool Contains(char pChar) => pChar >= ' ' && pChar < cChar.DEL;
        }

        private class cRFC2045Token : cCharset
        {
            public override bool Contains(byte pByte)
            {
                if (pByte <= cASCII.SPACE) return false;
                if (pByte >= cASCII.DEL) return false;
                if (ZIsOneOf(pByte, kaTSpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar <= ' ') return false;
                if (pChar >= cChar.DEL) return false;
                if (ZIsOneOf(pChar, kcTSpecials)) return false;
                return true;
            }
        }

        private class cRFC2047Token : cCharset
        {
            private const string kcESpecials = "()<>@,;:\\\"/[]?.=";
            private static readonly cBytes kaESpecials = new cBytes(kcESpecials);

            public override bool Contains(byte pByte)
            {
                if (pByte <= cASCII.SPACE) return false;
                if (pByte >= cASCII.DEL) return false;
                if (ZIsOneOf(pByte, kaESpecials)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar <= ' ') return false;
                if (pChar >= cChar.DEL) return false;
                if (ZIsOneOf(pChar, kcESpecials)) return false;
                return true;
            }
        }

        private class cAttributeChar : cCharset
        {
            private const string kcNotSome = "*'%";
            private static readonly cBytes kaNotSome = new cBytes(kcNotSome);

            public override bool Contains(byte pByte)
            {
                if (pByte <= cASCII.SPACE) return false;
                if (pByte >= cASCII.DEL) return false;
                if (ZIsOneOf(pByte, kaTSpecials)) return false;
                if (ZIsOneOf(pByte, kaNotSome)) return false;
                return true;
            }

            public override bool Contains(char pChar)
            {
                if (pChar <= ' ') return false;
                if (pChar >= cChar.DEL) return false;
                if (ZIsOneOf(pChar, kcTSpecials)) return false;
                if (ZIsOneOf(pChar, kcNotSome)) return false;
                return true;
            }
        }

        private class cSpecials : cCharset
        {
            private const string kcSpecials = "()<>[]:;@\\,.\"";
            private static readonly cBytes kaSpecials = new cBytes(kcSpecials);

            public override bool Contains(byte pByte) => ZIsOneOf(pByte, kaSpecials);
            public override bool Contains(char pChar) => ZIsOneOf(pChar, kcSpecials);
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
        /**<summary>Represents the characters used in RFC 3986 'IP-literal'.</summary>*/
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
        /**<summary>Represents the characters A-Z, 0-9, a-z, +, /, and =.</summary>*/
        public static readonly cCharset Base64 = new cBase64();
        /**<summary>Represents the characters used in RFC 2047 Quoted-Printable.</summary>*/
        public static readonly cCharset QEncoding = new cQEncoding();
        /**<summary>Represents the characters used in RFC 6532 'obs-ctext'.</summary>*/
        public static readonly cCharset ObsCText = new cObsCText();
        /**<summary>Represents the characters used in RFC 6532 'atext'.</summary>*/
        public static readonly cCharset AText = new cAText();
        /**<summary>Represents the characters used in RFC 6532 'obs-qtext'.</summary>*/
        public static readonly cCharset ObsQText = new cObsQText();
        /**<summary>Represents the characters used in RFC 6532 'dtext'.</summary>*/
        public static readonly cCharset DText = new cDText();
        /**<summary>Represents the characters used in RFC 6532 'obs-dtext'.</summary>*/
        public static readonly cCharset ObsDText = new cObsDText();
        /**<summary>Represents the characters used in RFC 5322 'ftext'.</summary>*/
        public static readonly cCharset FText = new cFText();
        /**<summary>Represents the characters used in RFC 5234 WSP and RFC 6532 'VCHAR'.</summary>*/
        public static readonly cCharset WSPVChar = new cWSPVChar();
        /**<summary>Represents the characters used in RFC 6749 'VSCHAR'.</summary>*/
        public static readonly cCharset VSChar = new cVSChar();
        /**<summary>Represents the characters used in RFC 2045 'token'.</summary>*/
        public static readonly cCharset RFC2045Token = new cRFC2045Token();
        /**<summary>Represents the characters used in RFC 2047 'token'.</summary>*/
        public static readonly cCharset RFC2047Token = new cRFC2047Token();
        /**<summary>Represents the characters used in RFC 2231 'attribute-char'.</summary>*/
        public static readonly cCharset AttributeChar = new cAttributeChar();
        /**<summary>Represents the characters used in RFC 5322 'specials'.</summary>*/
        public static readonly cCharset Specials = new cSpecials();
    }
}