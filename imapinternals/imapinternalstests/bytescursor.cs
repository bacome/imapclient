using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternalstests
{
    [TestClass]
    public class cBytesCursorTests
    {
        [TestMethod]
        public void cBytesCursor_Tests()
        {
        }

        internal static void _Tests(cTrace.cContext pParentContext)
        {










        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cURLParts), nameof(_Tests));

            cURLParts lParts;

            // from rfc 2221

            if (!LTryParse("IMAP://MIKE@SERVER2/", out lParts) || !lParts.IsHomeServerReferral) throw new cTestsException("2221.1");
            if (!LTryParse("IMAP://user;AUTH=GSSAPI@SERVER2/", out lParts) || !lParts.IsHomeServerReferral) throw new cTestsException("2221.2");
            if (!LTryParse("IMAP://user;AUTH=*@SERVER2/", out lParts) || !lParts.IsHomeServerReferral) throw new cTestsException("2221.3");

            // from rfc 2193

            if (!LTryParse("IMAP://user;AUTH=*@SERVER2/SHARED/FOO", out lParts) || !lParts.IsMailboxReferral || lParts.MailboxPath != "SHARED/FOO") throw new cTestsException("2193.1");
            if (LTryParse("IMAP://user;AUTH=*@SERVER2/REMOTE IMAP://user;AUTH=*@SERVER3/REMOTE", out lParts)) throw new cTestsException("2193.2");

            // from rfc 5092

            if (!LTryParse("imap://minbari.example.org/gray-council;UIDVALIDITY=385759045/;UID=20/;PARTIAL=0.1024", out lParts)) throw new cTestsException("5092.1.1");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.1.2");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.1.3");

            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.mailboxname | fParts.uidvalidity | fParts.uid | fParts.partial | fParts.partiallength) ||
                !lParts.MustUseAnonymous ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "gray-council" ||
                lParts.UIDValidity.Value != 385759045 ||
                lParts.UID != 20 ||
                lParts.PartialOffset != 0 ||
                lParts.PartialLength != 1024
                )
                throw new cTestsException("5092.1.4");

            if (!LTryParse("imap://psicorp.example.org/~peter/%E6%97%A5%E6%9C%AC%E8%AA%9E/%E5%8F%B0%E5%8C%97", out lParts)) throw new cTestsException("5092.2");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.2.1");
            if (!lParts.IsMailboxReferral) throw new cTestsException("5092.2.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.mailboxname) ||
                !lParts.MustUseAnonymous ||
                lParts.Host != "psicorp.example.org" ||
                lParts.MailboxPath != "~peter/日本語/台北"
                )
                throw new cTestsException("5092.2.3");


            if (!LTryParse("imap://;AUTH=GSSAPI@minbari.example.org/gray-council/;uid=20/;section=1.2", out lParts)) throw new cTestsException("5092.3");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.3.1");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.3.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.mechanismname | fParts.host | fParts.mailboxname | fParts.uid | fParts.section) ||
                lParts.MustUseAnonymous ||
                lParts.MechanismName != "GSSAPI" ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "gray-council" ||
                lParts.UID != 20 ||
                lParts.Section != "1.2"
                )
                throw new cTestsException("5092.3.3");

            if (!LTryParse("imap://;AUTH=*@minbari.example.org/gray%20council?SUBJECT%20shadows", out lParts)) throw new cTestsException("5092.5");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.5.1");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.5.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.mechanismname | fParts.host | fParts.mailboxname | fParts.search) ||
                lParts.MustUseAnonymous ||
                lParts.MechanismName != null ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "gray council" ||
                lParts.Search != "SUBJECT shadows"
                )
                throw new cTestsException("5092.5.3");

            if (!LTryParse("imap://john;AUTH=*@minbari.example.org/babylon5/personel?charset%20UTF-8%20SUBJECT%20%7B14+%7D%0D%0A%D0%98%D0%B2%D0%B0%D0%BD%D0%BE%D0%B2%D0%B0", out lParts)) throw new cTestsException("5092.6");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.6.1");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.6.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.userid | fParts.mechanismname | fParts.host | fParts.mailboxname | fParts.search) ||
                lParts.MustUseAnonymous ||
                lParts.UserId != "john" ||
                lParts.MechanismName != null ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "babylon5/personel" ||
                lParts.Search != "charset UTF-8 SUBJECT {14+}\r\nИванова"
                )
                throw new cTestsException("5092.6.3");

            // URLAUTH - rfc 4467

            if (!LTryParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2", out lParts)) throw new cTestsException("4467.1");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || lParts.IsAuthorisable || lParts.IsAuthorised) throw new cTestsException("4467.1.1");

            if (!LTryParse("imap://example.com/Shared/;uid=20/;section=1.2;urlauth=submit+fred", out lParts)) throw new cTestsException("4467.2");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || lParts.IsAuthorisable || lParts.IsAuthorised) throw new cTestsException("4467.2.1");

            if (!LTryParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2;urlauth=submit+fred", out lParts)) throw new cTestsException("4467.3");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || !lParts.IsAuthorisable || lParts.IsAuthorised) throw new cTestsException("4467.3.1");

            if (!LTryParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2;urlauth=submit+fred:internal:91354a473744909de610943775f92038", out lParts)) throw new cTestsException("4467.4");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || lParts.IsAuthorisable || !lParts.IsAuthorised) throw new cTestsException("4467.4.1");



            if (!LTryParse("imap://fr%E2%82%aCd@xn--frd-l50a.com/INBOX/;uid=20/;section=1.2", out lParts)) throw new cTestsException("IDN.1");
            if (lParts.UserId != "fr€d") throw new cTestsException("IDN.1.1");
            if (lParts.Host != "xn--frd-l50a.com") throw new cTestsException("IDN.1.2");
            if (lParts.DisplayHost != "fr€d.com") throw new cTestsException("IDN.1.3");

            if (!LTryParse("imap://fr%E2%82%aCd@fr%E2%82%aCd.com/INBOX/;uid=20/;section=1.2", out lParts)) throw new cTestsException("IDN.2");
            if (lParts.UserId != "fr€d") throw new cTestsException("IDN.2.1");
            if (lParts.Host != "fr€d.com") throw new cTestsException("IDN.2.2");
            if (lParts.DisplayHost != "fr€d.com") throw new cTestsException("IDN.2.3");


            // expiry
            //  TODO

            // network-path
            //  TODO

            // absolute-path
            //  TODO

            // edge cases for the URL
            //  TODO

            bool LTryParse(string pURL, out cURLParts rParts)
            {
                var lCursor = new cBytesCursor(pURL);
                if (!Process(lCursor, out rParts, lContext)) return false;
                if (!lCursor.Position.AtEnd) return false;
                return true;
            }
        }








        [TestMethod]
            public void BytesCursor_SequenceSet()
            {
                // parsing and construction tests
                _Tests_1("*", "cSequenceSet(cAsterisk())", null, new cUIntList(new uint[] { 15 }), "cSequenceSet(cSequenceSetNumber(15))");
                _Tests_1("0", null, "0", null, null);
                _Tests_1("2,4:7,9,12:*", "cSequenceSet(cSequenceSetNumber(2),cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(7)),cSequenceSetNumber(9),cSequenceSetRange(cSequenceSetNumber(12),cAsterisk()))", null, new cUIntList(new uint[] { 2, 4, 5, 6, 7, 9, 12, 13, 14, 15 }), "cSequenceSet(cSequenceSetNumber(2),cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(7)),cSequenceSetNumber(9),cSequenceSetRange(cSequenceSetNumber(12),cSequenceSetNumber(15)))");
                _Tests_1("*:4,7:5", "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(4),cAsterisk()),cSequenceSetRange(cSequenceSetNumber(5),cSequenceSetNumber(7)))", null, new cUIntList(new uint[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }), "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(15)))");
            }

            private void _Tests_1(string pCursor, string pExpSeqSet, string pExpRemainder, cUIntList pExpList, string pExpSeqSet2)
            {
                var lCursor = new cBytesCursor(pCursor);

                if (lCursor.GetSequenceSet(true, out var lSequenceSet))
                {
                    string lSeqSet = lSequenceSet.ToString();
                    if (lSeqSet != pExpSeqSet) throw new cTestsException($"failed to get expected sequence set from {pCursor}: got '{lSeqSet}' vs expected '{pExpSeqSet}'");

                    if (!cUIntList.TryConstruct(lSequenceSet, 15, true, out var lTemp)) throw new cTestsException($"failed to get an uintlist from {lSequenceSet}");
                    if (pExpList.Count != lTemp.Count) throw new cTestsException($"failed to get expected uintlist from {lSequenceSet}");
                    var lList = new cUIntList(lTemp.OrderBy(i => i));
                    for (int i = 0; i < pExpList.Count; i++) if (pExpList[i] != lList[i]) throw new cTestsException($"failed to get expected uintlist from {lSequenceSet}");

                    string lSeqSet2 = cSequenceSet.FromUInts(lList).ToString();
                    if (lSeqSet2 != pExpSeqSet2) throw new cTestsException($"failed to get expected sequence set from {lList}: got '{lSeqSet2}' vs expected '{pExpSeqSet2}'");
                }
                else if (pExpSeqSet != null) throw new cTestsException($"failed to get a sequence set from {pCursor}");


                if (lCursor.Position.AtEnd)
                {
                    if (pExpRemainder == null) return;
                    throw new cTestsException($"expected a remainder from {pCursor}");
                }

                string lRemainder = lCursor.GetRestAsString();
                if (lRemainder != pExpRemainder) throw new cTestsException($"failed to get expected remainder set from {pCursor}: '{lRemainder}' vs '{pExpRemainder}'");
            }



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
            DateTimeOffset lDateTimeOffset;
            DateTime lDateTime;
            if (!lCursor.GetDate(out lDateTime) || lDateTime != new DateTime(1968, 4, 4)) throw new cTestsException("date form 1");
            if (!lCursor.GetDate(out lDateTime) || lDateTime != new DateTime(1968, 4, 5)) throw new cTestsException("date form 2");
            if (lCursor.GetDate(out lDateTime)) throw new cTestsException("date should have failed on no terminating quote");
            if (!lCursor.SkipBytes(new cBytes("\"5-aPr-1968X"))) throw new cTestsException("date skip");
            if (lCursor.GetDate(out lDateTime)) throw new cTestsException("date should have failed on days > 31");
            if (!lCursor.SkipBytes(new cBytes("32-apr-1968"))) throw new cTestsException("date skip 2");
            if (lCursor.GetDate(out lDateTime)) throw new cTestsException("date should have failed on no hypen");
            if (!lCursor.SkipBytes(new cBytes("014-Apr-1968"))) throw new cTestsException("date skip 3");
            if (lCursor.GetDate(out lDateTime)) throw new cTestsException("date should have failed on invalid month");
            if (!lCursor.SkipBytes(new cBytes("30-Apx-1968"))) throw new cTestsException("date skip 4");
            if (lCursor.GetDate(out lDateTime)) throw new cTestsException("date should have failed on no hypen (2)");
            if (!lCursor.SkipBytes(new cBytes("\"30-April-1968\""))) throw new cTestsException("date skip 5");
            if (lCursor.GetDate(out lDateTime)) throw new cTestsException("date should have failed on 4 digit year");
            if (!lCursor.SkipBytes(new cBytes("\"30-Apr-68\""))) throw new cTestsException("date skip 6");
            if (lCursor.GetDate(out lDateTime)) throw new cTestsException("date should have failed on invalid days per month");
            if (!lCursor.SkipBytes(new cBytes("\"31-Feb-1968\""))) throw new cTestsException("date skip 7");

            lCursor = MakeCursor("\" 4-apr-1968 23:59:59 +0000\"\"04-apr-1968 23:59:59 +1200\"\"28-apr-1968 23:59:59 +1130\"\"28-apr-1968 11:59:59 -1000\"");
            if (!lCursor.GetDateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 1");
            if (!lCursor.GetDateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 4, 11, 59, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 2");
            if (!lCursor.GetDateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 28, 12, 29, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 3");
            if (!lCursor.GetDateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 28, 21, 59, 59, DateTimeKind.Utc)) throw new cTestsException("datetime 4");

            // -0000
            lCursor = MakeCursor("\" 4-apr-1968 23:59:59 -0000\"");
            if (!lCursor.GetDateTime(out lDateTimeOffset, out lDateTime) || lDateTime != new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Unspecified)) throw new cTestsException("datetime 5");

            // more to do ...

            lCursor = MakeCursor("1968-04-04T23:59:59Z,1968-04-04T23:59:59+12:00,1968-04-28T23:59:59+11:30,1968-04-28T11:59:59-10:00");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 1.1");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 4, 11, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 1.2");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 28, 12, 29, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 1.3");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1968, 4, 28, 21, 59, 59, DateTimeKind.Utc)) throw new cTestsException("timestamp 1.4");

            // examples from rfc3339
            lCursor = new cBytesCursor("1985-04-12T23:20:50.52Z,1996-12-19T16:39:57-08:00,1990-12-31T23:59:60Z,1990-12-31T15:59:60-08:00,1937-01-01T12:00:27.87+00:20");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1985, 04, 12, 23, 20, 50, 520, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.1");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1996, 12, 20, 00, 39, 57, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.2");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1990, 12, 31, 23, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.3");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1990, 12, 31, 23, 59, 59, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.COMMA)) throw new cTestsException("timestamp 2.4");
            //if (!lCursor.GetTimeStamp(out lDate) || lDate != new DateTime(1937, 01, 01, 12, 19, 32, 130, DateTimeKind.Utc)) throw new cTestsException("timestamp 2.5");

            // -00:00
            lCursor = new cBytesCursor("1985-04-12T23:20:50.52-00:00");
            if (!lCursor.GetTimeStamp(out lDateTimeOffset, out lDateTime) || lDateTime != new DateTime(1985, 04, 12, 23, 20, 50, 520, DateTimeKind.Unspecified)) throw new cTestsException("timestamp 3.1");





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












        [Conditional("DEBUG")]
        public static void _Tests_RFC822(cTrace.cContext pParentContext)
        {
            cBytesCursor lCursor;
            string lString;
            DateTimeOffset lDateTimeOffset;
            DateTime lDateTime;
            cByteList lByteList;


            // tests for WSP

            lCursor = new cBytesCursor("x \t y \t\r\n\tz");
            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipRFC822WSP() || lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.y) || !lCursor.SkipRFC822FWS() || lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.z)) throw new cTestsException("skip wsp 1");
            lCursor = new cBytesCursor("x \t y \t\r\n\tz");
            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipRFC822FWS() || lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.y) || !lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.z)) throw new cTestsException("skip wsp 2");
            lCursor = new cBytesCursor("x \t\r\ny \t\r\n\t\r\n z");
            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipRFC822FWS() || lCursor.SkipRFC822FWS() || !lCursor.SkipBytes(new cBytes("\r\ny")) || !lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.z)) throw new cTestsException("skip wsp 3");

            lCursor = new cBytesCursor("Muhammed.(I am  the greatest) Ali\r\n @(the)Vegas.WBA");
            if (!lCursor.GetToken(cCharset.Atom, null, null, out lString) || lString != "Muhammed." || !lCursor.SkipRFC822CFWS() || !lCursor.GetToken(cCharset.Atom, null, null, out lString) || lString != "Ali" || !lCursor.SkipRFC822CFWS() || !lCursor.SkipByte(cASCII.AT) || !lCursor.SkipRFC822CFWS() || !lCursor.GetToken(cCharset.Atom, null, null, out lString) || lString != "Vegas.WBA") throw new cTestsException("skip cfws 1");
            lCursor = new cBytesCursor("(I am \r\n the(xx\\)\\\\\\() gre \t() \tatest)");
            if (!lCursor.SkipRFC822CFWS() || !lCursor.Position.AtEnd) throw new cTestsException("skip cfws 2");

            // TODO: more tests for failure cases 



            // tests for IMF date (these examples from rfc 5322)

            lCursor = new cBytesCursor("Fri, 21 Nov 1997 09:55:06 -0600  x  Tue, 1 Jul 2003 10:52:37 +0200    x    Thu, 13 Feb 1969 23:32:54 -0330    x  Thu,\r\n\t13\r\n\t  Feb\r\n\t    1969\r\n\t23:32\r\n\t\t\t-0330 (Newfoundland Time)   x   21 Nov 97 09:55:06 GMT    x     Fri, 21 Nov 1997 09(comment):   55  :  06 -0600    x");

            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1997, 11, 21, 15, 55, 06, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 1");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(2003, 7, 1, 8, 52, 37, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 2");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1969, 2, 14, 3, 02, 54, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 3");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1969, 2, 14, 3, 02, 00, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 4");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1997, 11, 21, 9, 55, 06, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 5");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1997, 11, 21, 15, 55, 06, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 6");

            lCursor = new cBytesCursor("21 Nov 1997 09:55:06 CST  x  1 Jul 2003 10:52:37 A    x    13 Feb 1969 23:32:54 GMT    x  Thu,\r\n\t13\r\n\t  Feb\r\n\t    1969\r\n\t23:32\r\n\t\t\t-0000 (Unspecified Zone)   x");

            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1997, 11, 21, 15, 55, 06, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 7");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime != new DateTime(2003, 7, 1, 10, 52, 37, DateTimeKind.Unspecified) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 8");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime.ToUniversalTime() != new DateTime(1969, 2, 13, 23, 32, 54, DateTimeKind.Utc) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 9");
            if (!lCursor.GetRFC822DateTime(out lDateTimeOffset, out lDateTime) || lDateTime != new DateTime(1969, 2, 13, 23, 32, 00, DateTimeKind.Unspecified) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 10");

            // TODO: more tests for failure cases and alphanumeric zones
            //   Wed, 17 Jul 1996 02:23:25 -0700 (PDT)



            // header values
            lCursor = new cBytesCursor("   \t  \r\nHeader    \t:      \r\n\t       \t\r\n\r\n");

            if (lCursor.GetRFC822FieldName(out lString) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "   \t  ") throw new cTestsException("header 1.1");
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || !lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "      \t       \t") throw new cTestsException("header 1.2");

            lCursor = new cBytesCursor("Header  \t  :      \r\n        \t\r\nFred");
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || !lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "              \t") throw new cTestsException("header 2.1");

            lCursor = new cBytesCursor("Header:\r\n  this  is  \r\n   the\tvalue     \t\r\n");
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "  this  is     the\tvalue     \t") throw new cTestsException("header 3.1");

            lCursor = new cBytesCursor("Header:\r\n   should   \r\n    fail    \t\r\n more stuff");
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || lCursor.GetRFC822FieldValue(out lByteList)) throw new cTestsException("header 4.1");


            // atoms
            lCursor = new cBytesCursor("   \t  \r\n Header    \tAtom(comment)      \r\nAt?Om\tAt!om:{Atom}       \t\r\n\r\n");

            if (!lCursor.GetRFC822Atom(out lString) || lString != "Header" || !lCursor.GetRFC822Atom(out lString) || lString != "Atom") throw new cTestsException("atom 1.1");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF)) throw new cTestsException("atom 1.2");
            if (!lCursor.GetRFC822Atom(out lString) || lString != "At?Om") throw new cTestsException("atom 1.3");
            if (!lCursor.GetRFC822Atom(out lString) || lString != "At!om") throw new cTestsException("atom 1.4");
            if (lCursor.GetRFC822Atom(out lString)) throw new cTestsException("atom 1.5.1");
            if (!lCursor.SkipByte(cASCII.COLON)) throw new cTestsException("atom 1.5.2");
            if (!lCursor.GetRFC822Atom(out lString) || lString != "{Atom}") throw new cTestsException("atom 1.6");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.Position.AtEnd) throw new cTestsException("atom 1.7");

            // quoted strings
            lCursor = new cBytesCursor("   \t  \r\n \"Header\r\n with FWS\"    \t\"Atom(not a \\\"comment\\\")\"      \r\n\"At?Om\"\t\"At!om:{Atom}\"    \"\r\n\tFWS beginning and end\r\n\t\"   \t\r\n\r\n");

            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "Header with FWS" || !lCursor.GetRFC822QuotedString(out lString) || lString != "Atom(not a \"comment\")") throw new cTestsException("quoted string 1.1");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF)) throw new cTestsException("quoted string 1.2");
            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "At?Om") throw new cTestsException("quoted string 1.3");
            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "At!om:{Atom}") throw new cTestsException("quoted string 1.4");
            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "\tFWS beginning and end\t") throw new cTestsException("quoted string 1.5");
            if (lCursor.GetRFC822QuotedString(out lString)) throw new cTestsException("quoted string 1.6.1");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.Position.AtEnd) throw new cTestsException("quoted string 1.6.2");

            // mix
            lCursor = new cBytesCursor("   \t  \r\n Header    \tA");
            if (lCursor.GetRFC822QuotedString(out lString) || lCursor.Position.Byte != 0 || !lCursor.GetRFC822Atom(out lString) || lString != "Header" || !lCursor.SkipByte(cASCII.A)) throw new cTestsException("mix 1.1");
            lCursor = new cBytesCursor("   \t  \r\n \"Header\"    \tA");
            if (lCursor.GetRFC822Atom(out lString) || lCursor.Position.Byte != 0 || !lCursor.GetRFC822QuotedString(out lString) || lString != "Header" || !lCursor.SkipByte(cASCII.A)) throw new cTestsException("mix 1.2");

            // domain literal
            lCursor = new cBytesCursor("   \t (there is a domain\r\n literal coming up(and\tit'll\r\n\tbe a good one))  \r\n [Header]      \r\n ( now with with FWS ) [  \t  \r\n\t the.name.com  \r\n     ]    (now with embedded FWS)  [  \t  \r\n\t the \t   name   \r\n   com  \r\n     ]   (with quotes and utf8)    [     \\[   fr€d     ]     (something invalid)     [    [   ]   \r\n");

            if (!lCursor.GetRFC822DomainLiteral(out lString) || lString != "[Header]") throw new cTestsException("domain literal 1.1");
            if (!lCursor.GetRFC822DomainLiteral(out lString) || lString != "[the.name.com]") throw new cTestsException("domain literal 1.2");
            if (!lCursor.GetRFC822DomainLiteral(out lString) || lString != "[the name com]") throw new cTestsException("domain literal 1.3");
            if (!lCursor.GetRFC822DomainLiteral(out lString) || lString != "[[ fr€d]") throw new cTestsException("domain literal 1.4");
            if (lCursor.GetRFC822DomainLiteral(out lString)) throw new cTestsException("domain literal 1.5");
            if (!lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "[    [   ]   ") throw new cTestsException("domain literal 1.6");

            // domain
            lCursor = new cBytesCursor("    \t   (first a dot-atom form)   fred.angus.bart   (now a obs form)    frxd  \t   .       angxs     .   \t  bxrt      (now a\r\n literal)     [    192.168.1.1     ]       \r\nNextHeader");

            if (!lCursor.GetRFC822Domain(out lString) || lString != "fred.angus.bart") throw new cTestsException("domain 1.1");
            if (!lCursor.GetRFC822Domain(out lString) || lString != "frxd.angxs.bxrt") throw new cTestsException("domain 1.2");
            if (!lCursor.GetRFC822Domain(out lString) || lString != "[192.168.1.1]") throw new cTestsException("domain 1.3");
            if (!lCursor.GetRFC822FieldValue(out lByteList) || lByteList.Count != 0) throw new cTestsException("domain 1.4");
            if (lCursor.GetRestAsString() != "NextHeader") throw new cTestsException("domain 1.5");

            // local part
            lCursor = new cBytesCursor("    \t   (first a dot-atom form)   fred.angus.bart   (now a obs form)    frxd  \t   .       angxs     .   \t  bxrt      (now a\r\n quoted string)     \"th€ local part as a string\"       (then a second obsolete form)     \"fr€d\"  \t   .       angxs     .   \t  \"bzrt\"        \r\n ");

            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "fred.angus.bart") throw new cTestsException("local part 1.1");
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "frxd.angxs.bxrt") throw new cTestsException("local part 1.2");
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "th€ local part as a string") throw new cTestsException("local part 1.3");
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "fr€d.angxs.bzrt") throw new cTestsException("local part 1.4");
            if (!lCursor.Position.AtEnd) throw new cTestsException("local part 1.5");

            lCursor = new cBytesCursor("    \t   (edge case)   fred.angus.bart    .    []         \r\nNextHeader");
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "fred.angus.bart") throw new cTestsException("local part 2.1");
            if (!lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != ".    []         ") throw new cTestsException("local part 2.2");

            // message id
            lCursor = new cBytesCursor("     \r\n\t   (one)  <1234@local.machine.example>      <5678.21-Nov-1997@example.com>    <testabcd.1234@silly.example>     <1234   @   local(blah)  .machine .example>    ");

            string lIdLeft;
            string lIdRight;

            if (!lCursor.GetRFC822MsgId(out lIdLeft, out lIdRight) || cTools.MessageId(lIdLeft, lIdRight) != "1234@local.machine.example") throw new cTestsException("msgid 1.1");
            if (!lCursor.GetRFC822MsgId(out lIdLeft, out lIdRight) || cTools.MessageId(lIdLeft, lIdRight) != "5678.21-Nov-1997@example.com") throw new cTestsException("msgid 1.2");
            if (!lCursor.GetRFC822MsgId(out lIdLeft, out lIdRight) || cTools.MessageId(lIdLeft, lIdRight) != "testabcd.1234@silly.example") throw new cTestsException("msgid 1.3");
            if (!lCursor.GetRFC822MsgId(out lIdLeft, out lIdRight) || cTools.MessageId(lIdLeft, lIdRight) != "1234@local.machine.example") throw new cTestsException("msgid 1.4");
        }

    }
}
