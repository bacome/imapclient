using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal static class cValidation
    {
        // validates input from the user of the library
        //  this code only outputs non-obsolete rfc 5322 syntax but it will accept obsolete syntax as input
        //   not all obsolete syntax is convertable to non-obsolete, so the code may fail on valid (by the obsolete standard) input

        public static bool IsDotAtomText(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            var lAtoms = pString.Split('.');
            foreach (var lAtom in lAtoms) if (lAtom.Length == 0 || !cCharset.AText.ContainsAll(lAtom)) return false;
            return true;
        }

        public static bool IsDomainLiteral(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.SkipByte(cASCII.LBRACKET)) return false;
            lCursor.GetToken(cCharset.WSPDText, null, null, out cByteList _);
            if (!lCursor.SkipByte(cASCII.RBRACKET)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsNoFoldLiteral(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.SkipByte(cASCII.LBRACKET)) return false;
            lCursor.GetToken(cCharset.DText, null, null, out cByteList _);
            if (!lCursor.SkipByte(cASCII.RBRACKET)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsSectionPart(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            var lCursor = new cBytesCursor(pString);

            while (true)
            {
                if (!lCursor.GetNZNumber(out _, out _)) return false;
                if (!lCursor.SkipByte(cASCII.DOT)) break;
            }

            return lCursor.Position.AtEnd;
        }

        public static bool TryParseLocalPart(string pString, out string rLocalPart)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            var lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC822LocalPart(out rLocalPart)) return false;
            if (lCursor.Position.AtEnd && cCharset.WSPVChar.ContainsAll(rLocalPart)) return true;
            rLocalPart = null;
            return false;
        }

        public static bool TryParseDomain(string pString, out string rDomain)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            var lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC822Domain(out rDomain)) return false;
            if (lCursor.Position.AtEnd && (IsDotAtomText(rDomain) || IsDomainLiteral(rDomain))) return true;
            rDomain = null;
            return false;
        }

        public static bool TryParseMsgId(string pString, out string rMessageId)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            var lCursor = new cBytesCursor(pString);

            if (!lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight)) { rMessageId = null; return false; }

            if (lCursor.Position.AtEnd && IsDotAtomText(lIdLeft) && (IsDotAtomText(lIdRight) || IsNoFoldLiteral(lIdRight)))
            {
                rMessageId = "<" + lIdLeft + "@" + lIdRight + ">";
                return true;
            }

            rMessageId = null;
            return false;
        }

        public static bool TryParseMsgIds(string pString, out cStrings rMessageIds)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            cBytesCursor lCursor = new cBytesCursor(pString);

            var lMessageIds = new List<string>();

            while (true)
            {
                if (!lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight)) break;
                if (!IsDotAtomText(lIdLeft) || (!IsDotAtomText(lIdRight) && !IsNoFoldLiteral(lIdRight))) { rMessageIds = null; return false; }
                lMessageIds.Add("<" + lIdLeft + "@" + lIdRight + ">");
            }

            if (lCursor.Position.AtEnd && lMessageIds.Count > 0)
            {
                rMessageIds = new cStrings(lMessageIds);
                return true;
            }

            rMessageIds = null;
            return false;
        }

        public static bool TryParsePhrase(string pString, out cHeaderFieldPhraseValue rPhrase)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            cBytesCursor lCursor = new cBytesCursor(pString);

            if (!lCursor.GetRFC822Phrase(out rPhrase)) return false;
            if (lCursor.Position.AtEnd && ZIsValidRFC5322Phrase(rPhrase)) return true;

            rPhrase = null;
            return false;
        }

        public static bool TryParsePhrases(string pString, out List<cHeaderFieldPhraseValue> rPhrases)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            cBytesCursor lCursor = new cBytesCursor(pString);

            rPhrases = new List<cHeaderFieldPhraseValue>();

            while (true)
            {
                if (lCursor.GetRFC822Phrase(out var lPhrase))
                {
                    if (!ZIsValidRFC5322Phrase(lPhrase))
                    {
                        rPhrases = null;
                        return false;
                    }

                    rPhrases.Add(lPhrase);
                }
                else lCursor.SkipRFC822CFWS();

                if (!lCursor.SkipByte(cASCII.COMMA)) break;
            }

            if (lCursor.Position.AtEnd && rPhrases.Count > 0) return true;

            rPhrases = null;
            return false;
        }

        private static bool ZIsValidRFC5322Phrase(cHeaderFieldPhraseValue pPhrase)
        {
            foreach (var lPart in pPhrase.Parts)
            {
                switch (lPart)
                {
                    case cHeaderFieldCommentValue lComment:

                        if (ZIsValidRFC5322Comment(lComment)) break;
                        return false;

                    case cHeaderFieldTextValue lText:

                        if (cCharset.WSPVChar.ContainsAll(lText.Text)) break;
                        return false;

                    case cHeaderFieldQuotedStringValue lQuotedString:

                        if (cCharset.WSPVChar.ContainsAll(lQuotedString.Text)) break;
                        return false;

                    default:

                        throw new cInternalErrorException(nameof(cValidation), nameof(ZIsValidRFC5322Phrase));
                }
            }

            return true;
        }

        private static bool ZIsValidRFC5322Comment(cHeaderFieldCommentValue pComment)
        {
            foreach (var lPart in pComment.Parts)
            {
                switch (lPart)
                {
                    case cHeaderFieldCommentValue lComment:

                        if (ZIsValidRFC5322Comment(lComment)) break;
                        return false;

                    case cHeaderFieldTextValue lText:

                        if (cCharset.WSPVChar.ContainsAll(lText.Text)) break;
                        return false;

                    default:

                        throw new cInternalErrorException(nameof(cValidation), nameof(ZIsValidRFC5322Comment));
                }
            }

            return true;
        }

        internal static void _Tests()
        {
            if (!IsDotAtomText("fred.angus.mike")) throw new cTestsException($"{nameof(cValidation)}.IsDotAtom.1");
            if (!IsDotAtomText("fred")) throw new cTestsException($"{nameof(cValidation)}.IsDotAtom.2");
            if (IsDotAtomText("fred..angus.mike")) throw new cTestsException($"{nameof(cValidation)}.IsDotAtom.3");
            if (IsDotAtomText("")) throw new cTestsException($"{nameof(cValidation)}.IsDotAtom.4");
            if (IsDotAtomText("fred,angus.mike")) throw new cTestsException($"{nameof(cValidation)}.IsDotAtom.5");

            if (!IsDomainLiteral("[192.168.1.1]")) throw new cTestsException($"{nameof(cValidation)}.IsDomainLiteral.1");
            if (!IsDomainLiteral("[ 192 . \t 168 . 1 . 1 ]")) throw new cTestsException($"{nameof(cValidation)}.IsDomainLiteral.2");
            if (!IsDomainLiteral("[]")) throw new cTestsException($"{nameof(cValidation)}.IsDomainLiteral.3");
            if (IsDomainLiteral("[")) throw new cTestsException($"{nameof(cValidation)}.IsDomainLiteral.4");
            if (IsDomainLiteral("[[]")) throw new cTestsException($"{nameof(cValidation)}.IsDomainLiteral.5");
            if (IsDomainLiteral("[192.168.1.1] ")) throw new cTestsException($"{nameof(cValidation)}.IsDomainLiteral.6");

            if (!IsNoFoldLiteral("[192.168.1.1]")) throw new cTestsException($"{nameof(cValidation)}.IsNoFoldLiteral.1");
            if (IsNoFoldLiteral("[ 192 . \t 168 . 1 . 1 ]")) throw new cTestsException($"{nameof(cValidation)}.IsNoFoldLiteral.2");
            if (!IsNoFoldLiteral("[]")) throw new cTestsException($"{nameof(cValidation)}.IsNoFoldLiteral.3");
            if (IsNoFoldLiteral("[")) throw new cTestsException($"{nameof(cValidation)}.IsNoFoldLiteral.4");
            if (IsNoFoldLiteral("[[]")) throw new cTestsException($"{nameof(cValidation)}.IsNoFoldLiteral.5");
            if (IsNoFoldLiteral("[192.168.1.1] ")) throw new cTestsException($"{nameof(cValidation)}.IsNoFoldLiteral.6");

            string lString;

            if (!TryParseLocalPart(" fred   .   angus .   simon ", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.1");
            if (!TryParseLocalPart(" \"fred\"   .   angus .   simon ", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.2");
            if (TryParseLocalPart(" fred   .  .  angus .   simon ", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.3");
            if (!TryParseLocalPart(" \"fred..angus\" .   simon ", out lString) || lString != "fred..angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.4");
            if (!TryParseLocalPart(" \"fred.\\.angus\" .   simon ", out lString) || lString != "fred..angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.5");
            if (TryParseLocalPart(" \"fred.\\\angus\" .   simon ", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.6");
            if (!TryParseLocalPart("fred.angus.simon", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.7");
            if (!TryParseLocalPart("\"fred.angus.simon\"", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.8");
            if (!TryParseLocalPart("\"fred angus simon\"", out lString) || lString != "fred angus simon") throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.9");
            if (TryParseLocalPart("\"fred angus simon\"  .  ", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseLocalPart.10");

            if (!TryParseDomain(" fred   .   angus .   simon ", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.1");
            if (TryParseDomain(" \"fred\"   .   angus .   simon ", out lString) ) throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.2");
            if (TryParseDomain(" fred   .  .  angus .   simon ", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.3");
            if (!TryParseDomain("fred.angus.simon", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.4");
            if (!TryParseDomain(" [] ", out lString) || lString != "[]") throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.5");
            if (!TryParseDomain(" [ 192   . 168   . 1 \t   . 1 ] ", out lString) || lString != "[ 192   . 168   . 1 \t   . 1 ]") throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.6");
            if (!TryParseDomain("[192.168.1.1]", out lString) || lString != "[192.168.1.1]") throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.7");
            if (!TryParseDomain("[192.168\\.1.1]", out lString) || lString != "[192.168.1.1]") throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.8");
            if (TryParseDomain("[192.168\\\a1.1]", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.9");
            if (TryParseDomain("[192.168.1.1]a", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseDomain.10");

            if (!TryParseMsgId("<fred.angus.miles@simon.john.lemar>", out lString) || lString != "<fred.angus.miles@simon.john.lemar>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.1");
            if (!TryParseMsgId("  \t   <   \"fred\"    .  \t angus  \".miles\"  \r\n @ simon    .   john   \r\n .  lemar > \t     ", out lString) || lString != "<fred.angus.miles@simon.john.lemar>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.2");
            if (!TryParseMsgId("  \t   <   \"fred\"    .  \t angus  \".miles\"  \r\n @ [simon.john.lemar] > \t     ", out lString) || lString != "<fred.angus.miles@[simon.john.lemar]>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.3");
            if (TryParseMsgId("  \t   <   \"fred\"    .  \t angus  \".miles\"  \r\n @ [simon.john.lemar ] > \t     ", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.3.1");
            if (!TryParseMsgId("<this@one>", out lString) || lString != "<this@one>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.4");
            if (TryParseMsgId("<this@one><this@one>", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.5");
            if (!TryParseMsgId("<\"this\"@one>", out lString) || lString != "<this@one>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.6");
            if (TryParseMsgId("<\"th is\"@one>", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.7");
            if (!TryParseMsgId("<this@[on\\a]>", out lString) || lString != "<this@[ona]>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.8");
            if (TryParseMsgId("<this@[on\\\a]>", out lString)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgId.9");

            cStrings lStrings;
            if (!TryParseMsgIds("<this@one><this@one>", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@one>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.1");
            if (!TryParseMsgIds(" <  \r\n this @ one > \r\n <  \t this @  \t\t one > ", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@one>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.2");
            if (TryParseMsgIds("<this@one><this@one", out lStrings)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.3");
            if (TryParseMsgIds("", out lStrings)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.4");
            if (TryParseMsgIds("       ", out lStrings)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.5");
            if (!TryParseMsgIds("<this@one><\"this\"@one>", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@one>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.6");
            if (TryParseMsgIds("<this@one><\"this \"@one>", out lStrings)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.7");
            if (!TryParseMsgIds("<this@one><this@[one]>", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@[one]>") throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.8");
            if (!TryParseMsgIds("<this@one><this@[one ]>", out lStrings)) throw new cTestsException($"{nameof(cValidation)}.TryParseMsgIds.9");

            cHeaderFieldPhraseValue lPhrase;
            if (TryParsePhrase("", out lPhrase)) throw new cTestsException($"{ nameof(cValidation)}.TryParsePhrase.1");
            if (TryParsePhrase("     \t      ()    \t   (   \t stuff  \t  (more    \t stuff  ))    ", out lPhrase)) throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.1");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))    ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.2");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"xxx\"   (extra \"comment\")  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x xxx") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.3");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"x x\"   (extra \"comment\")  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x\"x x\"") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.4");
            if (!TryParsePhrase("   Arthur     C.    Clarke  (Author)  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "Arthur C. Clarke") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.5");
            if (TryParsePhrase("   .Arthur     C.    Clarke(, Author)  ", out lPhrase)) throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.6");
            if (TryParsePhrase("    Arthur     C.    Clarke,  Author   ", out lPhrase)) throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.7");
            if (!TryParsePhrase("   Arthur     C.  \"Clarke,\"Author   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "Arthur C. Clarke, Author") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.8");
            if (!TryParsePhrase("   A.         C.    Clarke   \t\t\t   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.9");
            if (!TryParsePhrase("  \"A.\"    \"C.\"  Clarke   \t\r\n   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.10");
            if (!TryParsePhrase("   A.         C.    Clarke   \"\"     ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke\"\"") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.11");
            if (TryParsePhrase(" \"\arthur\"   C.  \"Clarke,\"Author   ", out lPhrase)) throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.12");
            if (!TryParsePhrase("\" Arthur\"   C.    Clarke (\author)  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "\" Arthur\"C. Clarke") throw new cTestsException($"{nameof(cValidation)}.TryParsePhrase.13");

            // phrases
            List<cHeaderFieldPhraseValue> lPhrases;
            if (TryParsePhrases("", out lPhrases)) throw new cTestsException($"{ nameof(cValidation)}.TryParsePhrases.1");
            if (TryParsePhrases("  ,   (),       (comment) ,    (  \t \r\n longer  comment   ) ,    ", out lPhrases)) throw new cTestsException($"{ nameof(cValidation)}.TryParsePhrases.2");
            if (!TryParsePhrases(" ,   (),   x   (comment) ,    (  \t \r\n longer  comment   ) ,    ", out lPhrases) || lPhrases.Count != 1 || ZTestPhraseToString(lPhrases[0]) != "x") throw new cTestsException($"{ nameof(cValidation)}.TryParsePhrases.3");
        }

        private static string ZTestPhraseToString(cHeaderFieldPhraseValue pPhrase)
        {
            var lBuilder = new StringBuilder();

            foreach (var lPart in pPhrase.Parts)
            {
                switch (lPart)
                {
                    case cHeaderFieldCommentValue lComment:

                        lBuilder.Append(ZTestCommentToString(lComment));
                        break;

                    case cHeaderFieldTextValue lText:

                        lBuilder.Append(lText.Text);
                        break;

                    case cHeaderFieldQuotedStringValue lQuotedString:

                        lBuilder.Append('"');
                        lBuilder.Append(lQuotedString.Text);
                        lBuilder.Append('"');
                        break;

                    default:

                        throw new cInternalErrorException(nameof(cValidation), nameof(ZTestPhraseToString));
                }
            }

            return lBuilder.ToString();
        }

        private static string ZTestCommentToString(cHeaderFieldCommentValue pComment)
        {
            var lBuilder = new StringBuilder();

            lBuilder.Append('(');

            foreach (var lPart in pComment.Parts)
            {
                switch (lPart)
                {
                    case cHeaderFieldCommentValue lComment:

                        lBuilder.Append(ZTestCommentToString(lComment));
                        break;

                    case cHeaderFieldTextValue lText:

                        lBuilder.Append(lText.Text);
                        break;

                    default:

                        throw new cInternalErrorException(nameof(cValidation), nameof(ZTestCommentToString));
                }
            }

            lBuilder.Append(')');

            return lBuilder.ToString();
        }
    }
}