using System;
using System.Collections.Generic;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public static class cMailValidation
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

            // try to parse it as an RFC822 format local-part
            var lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC822LocalPart(out rLocalPart)) return false;
            if (lCursor.Position.AtEnd && cCharset.WSPVChar.ContainsAll(rLocalPart)) return true;

            // try to treat it as a quoted string
            rLocalPart = pString.Trim();
            if (cCharset.WSPVChar.ContainsAll(rLocalPart)) return true;

            // can't think of anything else to try
            rLocalPart = null;
            return false;
        }

        public static bool TryParseDomain(string pString, out string rDomain)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            var lCursor = new cBytesCursor(pString);

            if (!lCursor.GetRFC822Domain(out var lDomain) || !lCursor.Position.AtEnd) { rDomain = null; return false; }

            if (IsDotAtomText(lDomain))
            {
                rDomain = cTools.GetDisplayHost(lDomain);
                return true;
            }

            if (IsDomainLiteral(lDomain)) { rDomain = lDomain; return true; } 

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
                else lCursor.SkipRFC822CFWS(); // to allow for blank elements in the list

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
    }
}