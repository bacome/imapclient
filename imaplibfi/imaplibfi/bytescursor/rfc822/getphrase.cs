using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public partial class cBytesCursor
    {
        public bool GetRFC822Phrase(out cHeaderFieldPhraseValue rPhrase)
        {
            const char kCleanChar = ' ';

            var lStrings = new List<string>();

            var lBookmark = Position;

            while (true)
            {
                if (ZGetRFC822DAtom(out var lString))
                {
                    if (lStrings.Count == 0 && lString[0] == '.')
                    {
                        Position = lBookmark;
                        rPhrase = null;
                        return false;
                    }

                    lStrings.Add(lString);
                }
                else if (GetRFC822QuotedString(out lString)) lStrings.Add(lString);
                else
                {
                    if (lStrings.Count == 0)
                    {
                        rPhrase = null;
                        return false;
                    }

                    break;
                }
            }

            var lBuilder = new StringBuilder();

            var lParts = new List<cHeaderFieldCommentTextQuotedStringValue>();

            foreach (var lString in lStrings)
            {
                if (lString.Length == 0 || cTools.ContainsWSP(lString))
                {
                    if (lBuilder.Length != 0)
                    {
                        cCharset.WSPVChar.CleanBuilder(lBuilder, kCleanChar);
                        lParts.Add(new cHeaderFieldTextValue(lBuilder.ToString()));
                        lBuilder.Clear();
                    }

                    if (lString.Length == 0) lParts.Add(cHeaderFieldQuotedStringValue.Empty);
                    else lParts.Add(new cHeaderFieldQuotedStringValue(cCharset.WSPVChar.CleanString(lString, kCleanChar)));
                }
                else
                {
                    if (lBuilder.Length != 0) lBuilder.Append(" ");
                    lBuilder.Append(lString);
                }
            }

            if (lBuilder.Length != 0)
            {
                cCharset.WSPVChar.CleanBuilder(lBuilder, kCleanChar);
                lParts.Add(new cHeaderFieldTextValue(lBuilder.ToString()));
            }

            rPhrase = new cHeaderFieldPhraseValue(lParts);
            return true;
        }
    }
}