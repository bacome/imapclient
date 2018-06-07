using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cMailboxPathPattern
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

            private readonly string mPrefixedWith;
            private readonly cStrings mNotPrefixedWith;
            private readonly sCursor mPattern;
            private readonly bool mNoDelimiter;
            private readonly char mDelimiter;

            public cMailboxPathPattern(string pPrefixedWith, cStrings pNotPrefixedWith, string pPattern, char? pDelimiter)
            {
                mPrefixedWith = pPrefixedWith ?? throw new ArgumentNullException(nameof(pPrefixedWith));
                mNotPrefixedWith = pNotPrefixedWith ?? throw new ArgumentNullException(nameof(pNotPrefixedWith));

                if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));
                mPattern = new sCursor(pPattern);

                if (pDelimiter == null) mNoDelimiter = true;
                else
                {
                    mNoDelimiter = false;
                    mDelimiter = pDelimiter.Value;
                }
            }

            public bool Matches(string pMailboxPath)
            {
                if (!pMailboxPath.StartsWith(mPrefixedWith)) return false;
                foreach (var lPrefix in mNotPrefixedWith) if (pMailboxPath.StartsWith(lPrefix)) return false;
                return ZMatchHere(mPattern, new sCursor(pMailboxPath, mPrefixedWith.Length));
            }

            private bool ZMatchHere(sCursor pPattern, sCursor pMailboxPath)
            {
                while (true)
                {
                    if (pPattern.AtEnd) return pMailboxPath.AtEnd;
                    char lPatternCurrent = pPattern.Current;
                    pPattern.MoveNext();

                    if (lPatternCurrent == '*') return ZMatchWildcard(true, pPattern, pMailboxPath);
                    if (lPatternCurrent == '%') return ZMatchWildcard(mNoDelimiter, pPattern, pMailboxPath);

                    if (pMailboxPath.AtEnd) return false;
                    if (lPatternCurrent != pMailboxPath.Current) return false;
                    pMailboxPath.MoveNext();
                }
            }

            private bool ZMatchWildcard(bool pMatchDelimiter, sCursor pPattern, sCursor pMailboxPath)
            {
                while (true)
                {
                    if (ZMatchHere(pPattern, pMailboxPath)) return true;
                    if (pMailboxPath.AtEnd) return false;
                    if (!pMatchDelimiter && pMailboxPath.Current == mDelimiter) return false;
                    pMailboxPath.MoveNext();
                }
            }

            public override string ToString() => $"{nameof(cMailboxPathPattern)}({mPrefixedWith},{mNotPrefixedWith},{mPattern},{mDelimiter})";

            [Conditional("DEBUG")]
            public static void _Tests(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cMailboxPathPattern), nameof(_Tests));

                cMailboxPathPattern lPattern;

                lPattern = new cMailboxPathPattern("", cStrings.Empty, "*", null);

                if (!lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                if (!lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                if (!lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");

                lPattern = new cMailboxPathPattern("", cStrings.Empty, "*", '/');

                if (!lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                if (!lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");

                lPattern = new cMailboxPathPattern("", cStrings.Empty, "fred", '/');

                if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                if (lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");

                lPattern = new cMailboxPathPattern("", cStrings.Empty, "fred*", '/');

                if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");

                lPattern = new cMailboxPathPattern("", cStrings.Empty, "*fred", '/');

                if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},2");
                if (lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},3");
                if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},4");
                if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},5");


                lPattern = new cMailboxPathPattern("", cStrings.Empty, "*fred*", '/');

                if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                if (!lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                if (!lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");

                lPattern = new cMailboxPathPattern("fred/", cStrings.Empty, "%", null);

                if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                if (!lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                if (!lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                if (!lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");


                lPattern = new cMailboxPathPattern("fred/", cStrings.Empty, "%", '/');

                if (lPattern.Matches("")) throw new cTestsException($"{lPattern},1");
                if (!lPattern.Matches("fred/angus")) throw new cTestsException($"{lPattern},2");
                if (lPattern.Matches("fred/angus/nigel")) throw new cTestsException($"{lPattern},3");
                if (lPattern.Matches("fred/angus/tom")) throw new cTestsException($"{lPattern},4");
                if (!lPattern.Matches("fred/nigel")) throw new cTestsException($"{lPattern},5");
                if (lPattern.Matches("fred/nigel/angus")) throw new cTestsException($"{lPattern},6");
                if (lPattern.Matches("~other/fred")) throw new cTestsException($"{lPattern},7");
                if (lPattern.Matches("~other/angus")) throw new cTestsException($"{lPattern},8");



                lPattern = new cMailboxPathPattern("", cStrings.Empty, "*/%g%/*", '/');

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