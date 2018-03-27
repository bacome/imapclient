using System;
using System.Collections.Generic;
using System.Text;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        public bool GetRFC822Phrase(out cHeaderFieldPhraseValue rPhrase)
        {
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
                        lParts.Add(new cHeaderFieldTextValue(lBuilder.ToString(), true));
                        lBuilder.Clear();
                    }

                    if (lString.Length == 0) lParts.Add(cHeaderFieldQuotedStringValue.Empty);
                    else lParts.Add(new cHeaderFieldQuotedStringValue(lString, true));
                }
                else
                {
                    if (lBuilder.Length != 0) lBuilder.Append(" ");
                    lBuilder.Append(lString);
                }
            }

            if (lBuilder.Length != 0) lParts.Add(new cHeaderFieldTextValue(lBuilder.ToString(), true));

            rPhrase = new cHeaderFieldPhraseValue(lParts);
            return true;
        }
    }
}