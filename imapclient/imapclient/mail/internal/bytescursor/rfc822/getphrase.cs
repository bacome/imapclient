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

            while (true)
            {
                string lString;

                var lBookmark = Position;

                if (ZGetRFC822DAtom(out lString))
                {
                    if (lStrings.Count == 0 && lString[0] == '.') Position = lBookmark;
                    else
                    {
                        lStrings.Add(lString);
                        continue;
                    }
                }

                if (!GetRFC822QuotedString(out lString)) break;

                lStrings.Add(lString);
            }

            if (lStrings.Count == 0) { rPhrase = null; return false; }

            var lBuilder = new StringBuilder();

            var lParts = new List<cHeaderFieldCommentTextQuotedStringValue>();

            foreach (var lString in lStrings)
            {
                if (lString.Length == 0)
                {
                    if (lBuilder.Length != 0)
                    {
                        lParts.Add(new cHeaderFieldTextValue(lBuilder.ToString(), true));
                        lBuilder.Clear();
                    }

                    lParts.Add(cHeaderFieldQuotedStringValue.Empty);
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