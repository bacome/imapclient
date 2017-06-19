using System;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cMailboxNamePattern
        {
            // concept from http://www.cs.princeton.edu/courses/archive/spr09/cos333/beautiful.html

            private struct sCursor
            {
                private string mString;
                private int mPosition;

                public sCursor(string pString)
                {
                    mString = pString;
                    mPosition = 0;
                }

                public sCursor(string pString, int pPosition)
                {
                    mString = pString;
                    mPosition = pPosition;
                }

                public bool AtEnd => mPosition == mString.Length;
                public char Current => mString[mPosition];
                public void MoveNext() => mPosition++;

                public override string ToString() => $"{nameof(sCursor)}({mString},{mPosition})";
            }

            private string mPrefix;
            private sCursor mPattern;
            private bool mNoDelimiter;
            private char mDelimiter;

            public cMailboxNamePattern(string pPrefix, string pPattern, char? pDelimiter)
            {
                if (pPattern == null) throw new ArgumentNullException(pPattern);

                mPrefix = pPrefix ?? throw new ArgumentNullException(pPrefix);
                mPattern = new sCursor(pPattern);

                if (pDelimiter == null) mNoDelimiter = true;
                else
                {
                    mNoDelimiter = false;
                    mDelimiter = pDelimiter.Value;
                }
            }

            public bool Matches(string pMailboxName)
            {
                if (!pMailboxName.StartsWith(mPrefix)) return false;
                return ZMatchHere(mPattern, new sCursor(pMailboxName, mPrefix.Length));
            }

            private bool ZMatchHere(sCursor pPattern, sCursor pMailboxName)
            {
                while (true)
                {
                    if (pPattern.AtEnd) return pMailboxName.AtEnd;
                    char lPatternCurrent = pPattern.Current;
                    pPattern.MoveNext();

                    if (lPatternCurrent == '*') return ZMatchWildcard(true, pPattern, pMailboxName);
                    if (lPatternCurrent == '%') return ZMatchWildcard(mNoDelimiter, pPattern, pMailboxName);

                    if (pMailboxName.AtEnd) return false;
                    if (lPatternCurrent != pMailboxName.Current) return false;
                    pMailboxName.MoveNext();
                }
            }

            private bool ZMatchWildcard(bool pMatchDelimiter, sCursor pPattern, sCursor pMailboxName)
            {
                while (true)
                {
                    if (ZMatchHere(pPattern, pMailboxName)) return true;
                    if (pMailboxName.AtEnd) return false;
                    if (!pMatchDelimiter && pMailboxName.Current == mDelimiter) return false;
                    pMailboxName.MoveNext();
                }
            }

            public override string ToString()
            {
                if (mNoDelimiter) return $"{nameof(cMailboxNamePattern)}({mPrefix},{mPattern})";
                return $"{nameof(cMailboxNamePattern)}({mPrefix},{mPattern},{mDelimiter})";
            }

            public static class cTests
            {
                [Conditional("DEBUG")]
                public static void Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewGeneric($"{nameof(cMailboxNamePattern)}.{nameof(cTests)}.{nameof(Tests)}");

                    cMailboxNamePattern lPattern;

                    lPattern = new cMailboxNamePattern("", "*", null);

                    if (!lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                    if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                    if (!lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                    if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                    if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                    if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                    if (!lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");

                    lPattern = new cMailboxNamePattern("", "*", '/');

                    if (!lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                    if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                    if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                    if (!lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");

                    lPattern = new cMailboxNamePattern("", "fred", '/');

                    if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                    if (lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                    if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                    if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");

                    lPattern = new cMailboxNamePattern("", "fred*", '/');

                    if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                    if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                    if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                    if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");

                    lPattern = new cMailboxNamePattern("", "*fred", '/');

                    if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                    if (lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                    if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                    if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");


                    lPattern = new cMailboxNamePattern("", "*fred*", '/');

                    if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                    if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                    if (!lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                    if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                    if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                    if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                    if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");

                    lPattern = new cMailboxNamePattern("fred/", "%", null);

                    if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                    if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                    if (!lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                    if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                    if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                    if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                    if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");


                    lPattern = new cMailboxNamePattern("fred/", "%", '/');

                    if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                    if (lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                    if (lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                    if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                    if (lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                    if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                    if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");



                    lPattern = new cMailboxNamePattern("", "*/%g%/*", '/');

                    if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                    if (lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                    if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                    if (!lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                    if (lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                    if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                    if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                    if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");
                }
            }
        }
    }
}