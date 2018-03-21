using System;
using System.Collections.Generic;
using System.Text;

namespace work.bacome.mailclient
{
    public static class cValidation
    {
        // validates input from the user of the library
        //  this code only outputs non-obsolete rfc 5322 syntax but it will accept some obsolete syntax on input
        //   not all obsolete syntax is convertable to non-obsolete, so the code may fail on valid (by the obsolete standard) input

        public static bool IsDotAtom(string pString, out string rDotAtomText)
        {
            if (pString == null) { rDotAtomText = null; return false; }
            var lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC5322DotAtomText(out rDotAtomText)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsDomainLiteral(string pString, out string rDomainLiteral)
        {
            if (pString == null) { rDomainLiteral = null; return false; }
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC5322DomainLiteral(out rDomainLiteral)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsDomain(string pString, out string rDomain)
        {
            if (pString == null) { rDomain = null; return false; }
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC5322DotAtomText(out rDomain) && !lCursor.GetRFC5322DomainLiteral(out rDomain)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsNoFoldLiteral(string pString, out string rNoFoldLiteral)
        {
            if (pString == null) { rNoFoldLiteral = null; return false; }
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC5322NoFoldLiteral(out rNoFoldLiteral)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsMessageId(string pString, out string rMessageId)
        {
            if (pString == null) { rMessageId = null; return false; }
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC5322MsgId(out rMessageId)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsMessageIds(string pString, out List<string> rMessageIds)
        {
            if (pString == null) { rMessageIds = null; return false; }

            cBytesCursor lCursor = new cBytesCursor(pString);

            rMessageIds = new List<string>();

            while (true)
            {
                if (!lCursor.GetRFC5322MsgId(out var lMessageId)) break;
                rMessageIds.Add(lMessageId);
            }

            if (lCursor.Position.AtEnd && rMessageIds.Count > 0) return true;

            rMessageIds = null;
            return false;
        }

        public static bool IsPhrase(string pString, out cHeaderPhraseValue rPhrase)
        {
            if (pString == null) { rPhrase = null; return false; }
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!ZGetPhrase(lCursor, out rPhrase)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsPhrases(string pString, out List<cHeaderPhraseValue> rPhrases)
        {
            if (pString == null) { rPhrases = null; return false; }

            cBytesCursor lCursor = new cBytesCursor(pString);

            rPhrases = new List<cHeaderPhraseValue>();

            while (true)
            {
                if (ZGetPhrase(lCursor, out var lPhrase)) rPhrases.Add(lPhrase);
                else lCursor.SkipRFC822CFWS();
                if (!lCursor.SkipByte(cASCII.COMMA)) break;
            }

            if (lCursor.Position.AtEnd && rPhrases.Count > 0) return true;

            rPhrases = null;
            return false;
        }

        private static bool ZGetPhrase(cBytesCursor pCursor, out cHeaderPhraseValue rPhrase)
        {
            var lStrings = new List<string>();

            while (true)
            {
                string lString;

                var lBookmark = pCursor.Position;

                if (pCursor.GetRFC822DAtom(out lString))
                {
                    if (lStrings.Count == 0 && lString[0] == '.') pCursor.Position = lBookmark;
                    else
                    {
                        lStrings.Add(lString);
                        continue;
                    }
                }

                if (!pCursor.GetRFC5322QuotedString(out lString)) break;

                lStrings.Add(lString);
            }

            if (lStrings.Count == 0) { rPhrase = null; return false; }

            var lBuilder = new StringBuilder();

            var lParts = new List<cHeaderCommentTextQuotedStringValue>();

            foreach (var lString in lStrings)
            {
                if (lString.Length == 0)
                {
                    if (lBuilder.Length != 0)
                    {
                        lParts.Add(new cHeaderTextValue(lBuilder.ToString()));
                        lBuilder.Clear();
                    }

                    lParts.Add(cHeaderQuotedStringValue.Empty);
                }
                else
                {
                    if (lBuilder.Length != 0) lBuilder.Append(" ");
                    lBuilder.Append(lString);
                }
            }

            if (lBuilder.Length != 0) lParts.Add(new cHeaderTextValue(lBuilder.ToString()));

            rPhrase = new cHeaderPhraseValue(lParts);
            return true;
        }




        internal static void _Tests()
        {
            string lString;
            if (!IsDotAtom("    fred    .    angus    . mike  ", out lString) || lString != "fred.angus.mike") throw new cTestsException($"{nameof(cValidation)}.IsDotAtom.1");
            if (!IsDotAtom("    fred    .    angus    . mike  ", out lString) || lString != "fred.angus.mike") throw new cTestsException($"{nameof(cValidation)}.IsDotAtom.1");



            ;?;
        }
    }
}