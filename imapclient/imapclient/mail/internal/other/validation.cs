using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal static class cMailValidation
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

                        throw new cInternalErrorException(nameof(cMailValidation), nameof(ZIsValidRFC5322Phrase));
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

                        throw new cInternalErrorException(nameof(cMailValidation), nameof(ZIsValidRFC5322Comment));
                }
            }

            return true;
        }

        internal static void _Tests()
        {
            if (!IsDotAtomText("fred.angus.mike")) throw new cTestsException($"{nameof(cMailValidation)}.IsDotAtom.1");
            if (!IsDotAtomText("fred")) throw new cTestsException($"{nameof(cMailValidation)}.IsDotAtom.2");
            if (IsDotAtomText("fred..angus.mike")) throw new cTestsException($"{nameof(cMailValidation)}.IsDotAtom.3");
            if (IsDotAtomText("")) throw new cTestsException($"{nameof(cMailValidation)}.IsDotAtom.4");
            if (IsDotAtomText("fred,angus.mike")) throw new cTestsException($"{nameof(cMailValidation)}.IsDotAtom.5");

            if (!IsDomainLiteral("[192.168.1.1]")) throw new cTestsException($"{nameof(cMailValidation)}.IsDomainLiteral.1");
            if (!IsDomainLiteral("[ 192 . \t 168 . 1 . 1 ]")) throw new cTestsException($"{nameof(cMailValidation)}.IsDomainLiteral.2");
            if (!IsDomainLiteral("[]")) throw new cTestsException($"{nameof(cMailValidation)}.IsDomainLiteral.3");
            if (IsDomainLiteral("[")) throw new cTestsException($"{nameof(cMailValidation)}.IsDomainLiteral.4");
            if (IsDomainLiteral("[[]")) throw new cTestsException($"{nameof(cMailValidation)}.IsDomainLiteral.5");
            if (IsDomainLiteral("[192.168.1.1] ")) throw new cTestsException($"{nameof(cMailValidation)}.IsDomainLiteral.6");

            if (!IsNoFoldLiteral("[192.168.1.1]")) throw new cTestsException($"{nameof(cMailValidation)}.IsNoFoldLiteral.1");
            if (IsNoFoldLiteral("[ 192 . \t 168 . 1 . 1 ]")) throw new cTestsException($"{nameof(cMailValidation)}.IsNoFoldLiteral.2");
            if (!IsNoFoldLiteral("[]")) throw new cTestsException($"{nameof(cMailValidation)}.IsNoFoldLiteral.3");
            if (IsNoFoldLiteral("[")) throw new cTestsException($"{nameof(cMailValidation)}.IsNoFoldLiteral.4");
            if (IsNoFoldLiteral("[[]")) throw new cTestsException($"{nameof(cMailValidation)}.IsNoFoldLiteral.5");
            if (IsNoFoldLiteral("[192.168.1.1] ")) throw new cTestsException($"{nameof(cMailValidation)}.IsNoFoldLiteral.6");

            string lString;

            if (!TryParseLocalPart(" fred   .   angus .   simon ", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.1");
            if (!TryParseLocalPart(" \"fred\"   .   angus .   simon ", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.2");
            if (TryParseLocalPart(" fred   .  .  angus .   simon ", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.3");
            if (!TryParseLocalPart(" \"fred..angus\" .   simon ", out lString) || lString != "fred..angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.4");
            if (!TryParseLocalPart(" \"fred.\\.angus\" .   simon ", out lString) || lString != "fred..angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.5");
            if (TryParseLocalPart(" \"fred.\\\angus\" .   simon ", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.6");
            if (!TryParseLocalPart("fred.angus.simon", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.7");
            if (!TryParseLocalPart("\"fred.angus.simon\"", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.8");
            if (!TryParseLocalPart("\"fred angus simon\"", out lString) || lString != "fred angus simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.9");
            if (TryParseLocalPart("\"fred angus simon\"  .  ", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseLocalPart.10");

            if (!TryParseDomain(" fred   .   angus .   simon ", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.1");
            if (TryParseDomain(" \"fred\"   .   angus .   simon ", out lString) ) throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.2");
            if (TryParseDomain(" fred   .  .  angus .   simon ", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.3");
            if (!TryParseDomain("fred.angus.simon", out lString) || lString != "fred.angus.simon") throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.4");
            if (!TryParseDomain(" [] ", out lString) || lString != "[]") throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.5");
            if (!TryParseDomain(" [ 192   . 168   . 1 \t   . 1 ] ", out lString) || lString != "[ 192   . 168   . 1 \t   . 1 ]") throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.6");
            if (!TryParseDomain("[192.168.1.1]", out lString) || lString != "[192.168.1.1]") throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.7");
            if (!TryParseDomain("[192.168\\.1.1]", out lString) || lString != "[192.168.1.1]") throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.8");
            if (TryParseDomain("[192.168\\\a1.1]", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.9");
            if (TryParseDomain("[192.168.1.1]a", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseDomain.10");

            if (!TryParseMsgId("<fred.angus.miles@simon.john.lemar>", out lString) || lString != "<fred.angus.miles@simon.john.lemar>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.1");
            if (!TryParseMsgId("  \t   <   \"fred\"    .  \t angus  \".miles\"  \r\n @ simon    .   john   \r\n .  lemar > \t     ", out lString) || lString != "<fred.angus.miles@simon.john.lemar>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.2");
            if (!TryParseMsgId("  \t   <   \"fred\"    .  \t angus  \".miles\"  \r\n @ [simon.john.lemar] > \t     ", out lString) || lString != "<fred.angus.miles@[simon.john.lemar]>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.3");
            if (TryParseMsgId("  \t   <   \"fred\"    .  \t angus  \".miles\"  \r\n @ [simon.john.lemar ] > \t     ", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.3.1");
            if (!TryParseMsgId("<this@one>", out lString) || lString != "<this@one>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.4");
            if (TryParseMsgId("<this@one><this@one>", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.5");
            if (!TryParseMsgId("<\"this\"@one>", out lString) || lString != "<this@one>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.6");
            if (TryParseMsgId("<\"th is\"@one>", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.7");
            if (!TryParseMsgId("<this@[on\\a]>", out lString) || lString != "<this@[ona]>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.8");
            if (TryParseMsgId("<this@[on\\\a]>", out lString)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgId.9");

            cStrings lStrings;
            if (!TryParseMsgIds("<this@one><this@one>", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@one>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.1");
            if (!TryParseMsgIds(" <  \r\n this @ one > \r\n <  \t this @  \t\t one > ", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@one>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.2");
            if (TryParseMsgIds("<this@one><this@one", out lStrings)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.3");
            if (TryParseMsgIds("", out lStrings)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.4");
            if (TryParseMsgIds("       ", out lStrings)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.5");
            if (!TryParseMsgIds("<this@one><\"this\"@one>", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@one>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.6");
            if (TryParseMsgIds("<this@one><\"this \"@one>", out lStrings)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.7");
            if (!TryParseMsgIds("<this@one><this@[one]>", out lStrings) || lStrings.Count != 2 || lStrings[0] != "<this@one>" || lStrings[1] != "<this@[one]>") throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.8");
            if (!TryParseMsgIds("<this@one><this@[one ]>", out lStrings)) throw new cTestsException($"{nameof(cMailValidation)}.TryParseMsgIds.9");

            cHeaderFieldPhraseValue lPhrase;
            if (TryParsePhrase("", out lPhrase)) throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrase.1");
            if (TryParsePhrase("     \t      ()    \t   (   \t stuff  \t  (more    \t stuff  ))    ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.1");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))    ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.2");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"xxx\"   (extra \"comment\")  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x xxx") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.3");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"x x\"   (extra \"comment\")  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x\"x x\"") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.4");
            if (!TryParsePhrase("   Arthur     C.    Clarke  (Author)  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "Arthur C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.5");
            if (TryParsePhrase("   .Arthur     C.    Clarke(, Author)  ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.6");
            if (TryParsePhrase("    Arthur     C.    Clarke,  Author   ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.7");
            if (!TryParsePhrase("   Arthur     C.  \"Clarke,\"Author   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "Arthur C. Clarke, Author") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.8");
            if (!TryParsePhrase("   A.         C.    Clarke   \t\t\t   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.9");
            if (!TryParsePhrase("  \"A.\"    \"C.\"  Clarke   \t\r\n   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.10");
            if (!TryParsePhrase("   A.         C.    Clarke   \"\"     ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke\"\"") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.11");
            if (TryParsePhrase(" \"\arthur\"   C.  \"Clarke,\"Author   ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.12");
            if (!TryParsePhrase("\" Arthur\"   C.    Clarke (\author)  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "\" Arthur\"C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.13");

            // phrases
            List<cHeaderFieldPhraseValue> lPhrases;
            if (TryParsePhrases("", out lPhrases)) throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrases.1");
            if (TryParsePhrases("  ,   (),       (comment) ,    (  \t \r\n longer  comment   ) ,    ", out lPhrases)) throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrases.2");
            if (!TryParsePhrases(" ,   (),   x   (comment) ,    (  \t \r\n longer  comment   ) ,    ", out lPhrases) || lPhrases.Count != 1 || ZTestPhraseToString(lPhrases[0]) != "x") throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrases.3");
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

                        throw new cInternalErrorException(nameof(cMailValidation), nameof(ZTestPhraseToString));
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

                        throw new cInternalErrorException(nameof(cMailValidation), nameof(ZTestCommentToString));
                }
            }

            lBuilder.Append(')');

            return lBuilder.ToString();
        }
    }
}