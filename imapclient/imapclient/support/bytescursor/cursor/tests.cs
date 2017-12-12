using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal partial class cBytesCursor
    {
        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(_Tests));

            cBytesCursor lCursor;
            sPosition lBookmark;
            IList<byte> lBytes;

            lCursor = MakeCursor("", "", "");
            if (!lCursor.Position.AtEnd) throw new cTestsException("responsecursor - not at end");

            lCursor = MakeCursor("", "", "{ ", "");
            if (lCursor.Position.AtEnd) throw new cTestsException("responsecursor - not at end");
            if (lCursor.SkipByte(cASCII.SPACE)) throw new cTestsException("responsecursor - shouldn't skip literal");

            lCursor = MakeCursor("", "", " ", "");
            if (!lCursor.SkipByte(cASCII.SPACE)) throw new cTestsException("responsecursor - didn't skip space");
            if (!lCursor.Position.AtEnd) throw new cTestsException("responsecursor - not at end");

            lCursor = MakeCursor("", "AB", "CD", "");
            if (lCursor.SkipBytes(new cBytes("ABCDE"))) throw new cTestsException("responsecursor - found ABCDE");
            if (!lCursor.SkipBytes(new cBytes("ABCD"))) throw new cTestsException("responsecursor - no ABCD");
            if (!lCursor.Position.AtEnd) throw new cTestsException("responsecursor - not at end");

            lCursor = MakeCursor("ABCD", "{ABCD", "");

            lBookmark = lCursor.Position;

            if (!lCursor.SkipByte(cASCII.A)) throw new cTestsException("responsecursor - no A");
            lCursor.Position = lBookmark;
            if (!lCursor.SkipByte(cASCII.a)) throw new cTestsException("responsecursor - no a");
            lCursor.Position = lBookmark;
            if (lCursor.SkipByte(cASCII.a, true)) throw new cTestsException("responsecursor - a isn't A");
            if (!lCursor.SkipByte(cASCII.A) || !lCursor.SkipByte(cASCII.B)) throw new cTestsException("responsecursor - no AB");
            lCursor.Position = lBookmark;
            if (lCursor.SkipBytes(new cBytes("ABX"))) throw new cTestsException("responsecursor - found ABX");
            if (lCursor.SkipBytes(new cBytes("ABCDA"))) throw new cTestsException("responsecursor - found ABCDA");
            if (!lCursor.SkipBytes(new cBytes("AB")) || !lCursor.SkipBytes(new cBytes("CD"))) throw new cTestsException("responsecursor - no A");
            if (lCursor.SkipBytes(null)) throw new cTestsException("responsecursor - skip literal");
            if (lCursor.SkipByte(cASCII.A)) throw new cTestsException("responsecursor - skip literal");

            if (lCursor.GetString(out lBytes))
            {
                if (!cASCII.Compare(lBytes, new cBytes("ABCD"), false)) throw new cTestsException("responsecursor - not ABCD");
                if (!lCursor.Position.AtEnd) throw new cTestsException("responsecursor - not at end");
                if (lCursor.GetString(out lBytes)) throw new cTestsException("responsecursor - not at end");
            }
            else throw new cTestsException("responsecursor - no string");

            lCursor = MakeCursor("A", "B");
            if (!lCursor.SkipByte(cASCII.A) || !lCursor.SkipByte(cASCII.B)) throw new cTestsException("responsecursor - no AB");

            lCursor = MakeCursor("", "A", "", "B");
            if (!lCursor.SkipByte(cASCII.A) || !lCursor.SkipByte(cASCII.B)) throw new cTestsException("responsecursor - no AB");

            lCursor = MakeCursor("\"AB\\\\CD\\\"EF\" ");

            if (lCursor.GetString(out lBytes))
            {
                if (!cASCII.Compare(lBytes, new cBytes("AB\\CD\"EF"), false)) throw new cTestsException("responsecursor - not AB\\CD\"EF");
                if (lCursor.Position.AtEnd) throw new cTestsException("responsecursor - at end");
                if (lCursor.GetString(out lBytes)) throw new cTestsException("responsecursor - not at end");
                if (!lCursor.SkipByte(cASCII.SPACE)) throw new cTestsException("responsecursor - no space");
                if (!lCursor.Position.AtEnd) throw new cTestsException("responsecursor - not at end");
            }
            else throw new cTestsException("responsecursor - no string");

            lCursor = MakeCursor("\"AB\\\\CD\\\"EF");
            if (lCursor.GetString(out lBytes)) throw new cTestsException("responsecursor - found a string");
            if (!lCursor.SkipByte(cASCII.DQUOTE) || !lCursor.SkipByte(cASCII.A)) throw new cTestsException("responsecursor - no \"A");

            lCursor = MakeCursor("AT", "OM]ATOM(", "{literal", "ATOM)ATOM{ATOM%ATOM*ATOM\"ATOM\\ATOM ASTRIN]G");

            cByteList lToken;

            lBookmark = lCursor.Position;
            if (!lCursor.GetToken(cCharset.Atom, null, null, out lToken) || !cASCII.Compare(lToken, new cBytes("aToM"), false) || !lCursor.SkipByte(cASCII.RBRACKET)) throw new cTestsException("responsecursor - not atom");

            lCursor.Position = lBookmark;
            if (!lCursor.GetToken(cCharset.AString, null, null, out lToken) || !cASCII.Compare(lToken, new cBytes("aToM]atom"), false) || !lCursor.SkipByte(cASCII.LPAREN)) throw new cTestsException("responsecursor - not atom");

            if (lCursor.GetToken(cCharset.TextNotRBRACKET, null, null, out lToken)) throw new cTestsException("responsecursor - literal");
            if (!lCursor.GetString(out lBytes) || !cASCII.Compare(lBytes, new cBytes("literal"), false)) throw new cTestsException("responsecursor - literal");

            if (lCursor.GetToken(cCharset.Atom, null, null, out lToken, 5)) throw new cTestsException("min length");
            if (!lCursor.GetToken(cCharset.Atom, null, null, out lToken, 1, 2) || !cASCII.Compare(lToken, new cBytes("AT"), false)) throw new cTestsException("max length");

            lCursor = MakeCursor("fr%E2%82%acd");
            lBookmark = lCursor.Position;
            string lString;

            if (!lCursor.GetToken(cCharset.TextNotRBRACKET, null, null, out lString)) throw new cTestsException("didn't get string 1");
            if (lString != "fr%E2%82%acd") throw new cTestsException("didn't get right string 1");

            lCursor.Position = lBookmark;
            if (!lCursor.GetToken(cCharset.TextNotRBRACKET, cASCII.PERCENT, null, out lToken)) throw new cTestsException("didn't get string 2");
            if (lToken.Count != 6 || lToken[0] != cASCII.f || lToken[1] != cASCII.r || lToken[2] != 226 || lToken[3] != 130 || lToken[4] != 172 || lToken[5] != cASCII.d) throw new cTestsException("didn't get right string 2");

            lCursor.Position = lBookmark;
            if (!lCursor.GetToken(cCharset.TextNotRBRACKET, cASCII.PERCENT, null, out lString)) throw new cTestsException("didn't get string 3");
            if (lString != "fr€d") throw new cTestsException("didn't get right string 3");

            lCursor = MakeCursor("A1", "2345", "", "67890123");

            uint lNumber;
            if (lCursor.GetNumber(out _, out lNumber)) throw new cTestsException("A is not a digit");
            if (!lCursor.SkipByte(cASCII.A)) throw new cTestsException("A not skipped");
            if (lCursor.GetNumber(out _, out lNumber)) throw new cTestsException("number should be too big");
            if (!lCursor.GetNumber(out _, out lNumber, 1, 9) || lNumber != 123456789) throw new cTestsException("number max length");
            if (lCursor.GetNZNumber(out _, out lNumber)) throw new cTestsException("nznumber 1");
            if (!lCursor.SkipByte(cASCII.ZERO) || !lCursor.GetNZNumber(out _, out lNumber) || lNumber != 123) throw new cTestsException("nznumber 2");

            lCursor = MakeCursor("04-Apr-1968\"5-APR-1968\"\"5-APR-1968x32-apr-1968014-Apr-196830-Apx-1968\"30-April-1968\"\"30-Apr-68\"\"31-Feb-1968\"");
            DateTime lDate;
            if (!lCursor.GetDate(out lDate) || lDate != new DateTime(1968, 4, 4, 0, 0, 0, DateTimeKind.Utc)) throw new cTestsException("date form 1");
            if (!lCursor.GetDate(out lDate) || lDate != new DateTime(1968, 4, 5, 0, 0, 0, DateTimeKind.Utc)) throw new cTestsException("date form 2");
            if (lCursor.GetDate(out lDate)) throw new cTestsException("date should have failed on no terminating quote");
            if (!lCursor.SkipBytes(new cBytes("\"5-aPr-1968X"))) throw new cTestsException("date skip");
            if (lCursor.GetDate(out lDate)) throw new cTestsException("date should have failed on days > 31");
            if (!lCursor.SkipBytes(new cBytes("32-apr-1968"))) throw new cTestsException("date skip 2");
            if (lCursor.GetDate(out lDate)) throw new cTestsException("date should have failed on no hypen");
            if (!lCursor.SkipBytes(new cBytes("014-Apr-1968"))) throw new cTestsException("date skip 3");
            if (lCursor.GetDate(out lDate)) throw new cTestsException("date should have failed on invalid month");
            if (!lCursor.SkipBytes(new cBytes("30-Apx-1968"))) throw new cTestsException("date skip 4");
            if (lCursor.GetDate(out lDate)) throw new cTestsException("date should have failed on no hypen (2)");
            if (!lCursor.SkipBytes(new cBytes("\"30-April-1968\""))) throw new cTestsException("date skip 5");
            if (lCursor.GetDate(out lDate)) throw new cTestsException("date should have failed on 4 digit year");
            if (!lCursor.SkipBytes(new cBytes("\"30-Apr-68\""))) throw new cTestsException("date skip 6");
            if (lCursor.GetDate(out lDate)) throw new cTestsException("date should have failed on invalid days per month");
            if (!lCursor.SkipBytes(new cBytes("\"31-Feb-1968\""))) throw new cTestsException("date skip 7");

            lCursor = MakeCursor("\" 4-apr-1968 23:59:59 +0000\"\"04-apr-1968 23:59:59 +1200\"\"28-apr-1968 23:59:59 +1130\"\"28-apr-1968 11:59:59 -1000\"");
            if (!lCursor.GetDateTime(out lDate) || lDate != new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 1");
            if (!lCursor.GetDateTime(out lDate) || lDate != new DateTime(1968, 4, 4, 11, 59, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 2");
            if (!lCursor.GetDateTime(out lDate) || lDate != new DateTime(1968, 4, 28, 12, 29, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 3");
            if (!lCursor.GetDateTime(out lDate) || lDate != new DateTime(1968, 4, 28, 21, 59, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 4");

            // more to do ...

            lCursor = MakeCursor("1968-04-04T23:59:59Z,1968-04-04T23:59:59+12:00,1968-04-28T23:59:59+11:30,1968-04-28T11:59:59-10:00");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 1.1");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1968, 4, 4, 11, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 1.2");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1968, 4, 28, 12, 29, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 1.3");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1968, 4, 28, 21, 59, 59, DateTimeKind.Utc)) throw new cTestsException("timestamp 1.4");

            // examples from rfc3339
            lCursor = new cBytesCursor("1985-04-12T23:20:50.52Z,1996-12-19T16:39:57-08:00,1990-12-31T23:59:60Z,1990-12-31T15:59:60-08:00,1937-01-01T12:00:27.87+00:20");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1985, 04, 12, 23, 20, 50, 520, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.1");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1996, 12, 20, 00, 39, 57, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.2");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1990, 12, 31, 23, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.3");
            if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1990, 12, 31, 23, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.4");
            //if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1937, 01, 01, 12, 19, 32, 130, DateTimeKind.Utc)) throw new cTestsException("timestamp 2.5");








            _Tests_Capability(lContext);
            _Tests_SequenceSet(lContext);
            _Tests_RFC822(lContext);




            // <todo: getastring



            cBytesCursor MakeCursor(params string[] pLines)
            {
                List<cResponseLine> lLines = new List<cResponseLine>();

                foreach (var lLine in pLines)
                {
                    if (lLine.Length > 0 && lLine[0] == '{') lLines.Add(new cResponseLine(true, new cBytes(lLine.TrimStart('{'))));
                    else lLines.Add(new cResponseLine(false, new cBytes(lLine)));
                }

                return new cBytesCursor(new cResponse(lLines));
            }
        }
    }
}