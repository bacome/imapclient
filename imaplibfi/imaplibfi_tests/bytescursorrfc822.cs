using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cBytesCursor_RFC822
    {
        [TestMethod]
        public void cBytesCursor_RFC822_WSP()
        {
            string lString;
            cBytesCursor lCursor;

            lCursor = new cBytesCursor("x \t y \t\r\n\tz");
            Assert.IsTrue(lCursor.SkipByte(cASCII.x));
            Assert.IsTrue(lCursor.SkipRFC822WSP());
            Assert.IsFalse(lCursor.SkipRFC822WSP());
            Assert.IsTrue(lCursor.SkipByte(cASCII.y));
            Assert.IsTrue(lCursor.SkipRFC822FWS());
            Assert.IsFalse(lCursor.SkipRFC822FWS());
            Assert.IsTrue(lCursor.SkipByte(cASCII.z));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = new cBytesCursor("x \t y \t\r\n\tz");
            Assert.IsTrue(lCursor.SkipByte(cASCII.x));
            Assert.IsTrue(lCursor.SkipRFC822FWS());
            Assert.IsFalse(lCursor.SkipRFC822FWS());
            Assert.IsTrue(lCursor.SkipByte(cASCII.y));
            Assert.IsTrue(lCursor.SkipRFC822FWS());
            Assert.IsTrue(lCursor.SkipByte(cASCII.z));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = new cBytesCursor("x \t\r\ny \t\r\n\t\r\n z");
            Assert.IsTrue(lCursor.SkipByte(cASCII.x));
            Assert.IsTrue(lCursor.SkipRFC822FWS());
            Assert.IsFalse(lCursor.SkipRFC822FWS());
            Assert.IsTrue(lCursor.SkipBytes(new cBytes("\r\ny")));
            Assert.IsTrue(lCursor.SkipRFC822FWS());
            Assert.IsTrue(lCursor.SkipByte(cASCII.z));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = new cBytesCursor("Muhammed.(I am  the greatest) Ali\r\n @(the)Vegas.WBA");
            Assert.IsTrue(lCursor.GetToken(cCharset.Atom, null, null, out lString));
            Assert.AreEqual("Muhammed.", lString);
            Assert.IsTrue(lCursor.SkipRFC822CFWS());
            Assert.IsTrue(lCursor.GetToken(cCharset.Atom, null, null, out lString));
            Assert.AreEqual("Ali", lString);
            Assert.IsTrue(lCursor.SkipRFC822CFWS());
            Assert.IsTrue(lCursor.SkipByte(cASCII.AT));
            Assert.IsTrue(lCursor.SkipRFC822CFWS());
            Assert.IsTrue(lCursor.GetToken(cCharset.Atom, null, null, out lString));
            Assert.AreEqual("Vegas.WBA", lString);
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = new cBytesCursor("(I am \r\n the(xx\\)\\\\\\() gre \t() \tatest)");
            Assert.IsTrue(lCursor.SkipRFC822CFWS());
            Assert.IsTrue(lCursor.Position.AtEnd);


            // TODO: more tests for failure cases 
        }

        [TestMethod]
        public void cBytesCursor_RFC822_DateTime()
        {
            cTimestamp lTimestamp;
            cBytesCursor lCursor;

            lCursor = new cBytesCursor("Fri, 21 Nov 1997 09:55:06 -0600  x  Tue, 1 Jul 2003 10:52:37 +0200    x    Thu, 13 Feb 1969 23:32:54 -0330    x  Thu,\r\n\t13\r\n\t  Feb\r\n\t    1969\r\n\t23:32\r\n\t\t\t-0330 (Newfoundland Time)   x   21 Nov 97 09:55:06 GMT    x     Fri, 21 Nov 1997 09(comment):   55  :  06 -0600    x");

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1997, 11, 21, 15, 55, 06, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(2003, 7, 1, 8, 52, 37, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1969, 2, 14, 3, 02, 54, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1969, 2, 14, 3, 02, 00, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1997, 11, 21, 9, 55, 06, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1997, 11, 21, 15, 55, 06, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));
            Assert.IsTrue(lCursor.Position.AtEnd);




            lCursor = new cBytesCursor("21 Nov 1997 09:55:06 CST  x  1 Jul 2003 10:52:37 A    x    13 Feb 1969 23:32:54 GMT    x  Thu,\r\n\t13\r\n\t  Feb\r\n\t    1969\r\n\t23:32\r\n\t\t\t-0000 (Unspecified Zone)   x");

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1997, 11, 21, 15, 55, 06, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(2003, 7, 1, 10, 52, 37, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsTrue(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1969, 2, 13, 23, 32, 54, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsFalse(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));

            Assert.IsTrue(lCursor.GetRFC822DateTime(out lTimestamp));
            Assert.AreEqual(new DateTime(1969, 2, 13, 23, 32, 00, DateTimeKind.Utc), lTimestamp.UtcDateTime);
            Assert.IsTrue(lTimestamp.UnknownLocalOffset);

            Assert.IsTrue(lCursor.SkipByte(cASCII.x));
            Assert.IsTrue(lCursor.Position.AtEnd);


            // TODO: more tests for failure cases and alphanumeric zones
            //   Wed, 17 Jul 1996 02:23:25 -0700 (PDT)
        }

        [TestMethod]
        public void cBytesCursor_RFC822_Headers()
        {
            cBytesCursor lCursor;

            lCursor = new cBytesCursor("   \t  \r\nHeader    \t:      \r\n\t       \t\r\n\r\n");
            Assert.IsFalse(lCursor.GetRFC822FieldName(out _));
            ZSkipFieldValue(lCursor, "   \t  ");
            ZSkipFieldName(lCursor, "Header");
            Assert.IsTrue(lCursor.SkipRFC822WSP());
            Assert.IsTrue(lCursor.SkipByte(cASCII.COLON));
            ZSkipFieldValue(lCursor, "      \t       \t");

            lCursor = new cBytesCursor("Header  \t  :      \r\n        \t\r\nFred");
            ZSkipFieldName(lCursor, "Header");
            Assert.IsTrue(lCursor.SkipRFC822WSP());
            Assert.IsTrue(lCursor.SkipByte(cASCII.COLON));
            ZSkipFieldValue(lCursor, "              \t");

            lCursor = new cBytesCursor("Header:\r\n  this  is  \r\n   the\tvalue     \t\r\n");
            ZSkipFieldName(lCursor, "Header");
            Assert.IsFalse(lCursor.SkipRFC822WSP());
            Assert.IsTrue(lCursor.SkipByte(cASCII.COLON));
            ZSkipFieldValue(lCursor, "  this  is     the\tvalue     \t");

            lCursor = new cBytesCursor("Header:\r\n   should   \r\n    fail    \t\r\n more stuff");
            ZSkipFieldName(lCursor, "Header");
            Assert.IsFalse(lCursor.SkipRFC822WSP());
            Assert.IsTrue(lCursor.SkipByte(cASCII.COLON));
            Assert.IsFalse(lCursor.GetRFC822FieldValue(out _));

            lCursor = new cBytesCursor("     (something invalid)     [    [   ]   \r\n");
            ZSkipFieldValue(lCursor, "     (something invalid)     [    [   ]   ");
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = new cBytesCursor("\r\n");
            ZSkipFieldValue(lCursor, "");
            Assert.IsTrue(lCursor.Position.AtEnd);
        }

        private void ZSkipFieldName(cBytesCursor pCursor, string pExpectedName)
        {
            Assert.IsTrue(pCursor.GetRFC822FieldName(out var lFieldName));
            Assert.AreEqual(pExpectedName, lFieldName);
        }

        private void ZSkipFieldValue(cBytesCursor pCursor, string pExpectedValue)
        {
            Assert.IsTrue(pCursor.GetRFC822FieldValue(out var lFieldValue));
            Assert.AreEqual(pExpectedValue, cTools.UTF8BytesToString(lFieldValue));
        }

        [TestMethod]
        public void cBytesCursor_RFC822_Atoms()
        {
            var lCursor = new cBytesCursor("   \t  \r\n Header    \tAtom(comment)      \r\nAt?Om\tAt!om:{Atom}       \t\r\n\r\n");
            ZSkipAtom(lCursor, "Header");
            ZSkipAtom(lCursor, "Atom");
            Assert.IsTrue(lCursor.SkipByte(cASCII.CR));
            Assert.IsTrue(lCursor.SkipByte(cASCII.LF));
            ZSkipAtom(lCursor, "At?Om");
            ZSkipAtom(lCursor, "At!om");
            Assert.IsFalse(lCursor.GetRFC822Atom(out _));
            Assert.IsTrue(lCursor.SkipByte(cASCII.COLON));
            ZSkipAtom(lCursor, "{Atom}");
            Assert.AreEqual("\r\n\r\n", lCursor.GetRestAsString());

            lCursor = new cBytesCursor("   \t  \r\n Header    \tA");
            ZSkipAtom(lCursor, "Header");
            Assert.AreEqual("A", lCursor.GetRestAsString());

            lCursor = new cBytesCursor("   \t  \r\n \"Header\"    \tA");
            Assert.IsFalse(lCursor.GetRFC822Atom(out _));
            Assert.AreEqual(0, lCursor.Position.Byte);
        }

        private void ZSkipAtom(cBytesCursor pCursor, string pExpectedValue)
        {
            Assert.IsTrue(pCursor.GetRFC822Atom(out var lAtom));
            Assert.AreEqual(pExpectedValue, lAtom);
        }

        [TestMethod]
        public void cBytesCursor_RFC822_QuotedStrings()
        {
            var lCursor = new cBytesCursor("   \t  \r\n \"Header\r\n with FWS\"    \t\"Atom(not a \\\"comment\\\")\"      \r\n\"At?Om\"\t\"At!om:{Atom}\"    \"\r\n\tFWS beginning and end\r\n\t\"   \t\r\n\r\n");
            ZSkipQuotedString(lCursor, "Header with FWS");
            ZSkipQuotedString(lCursor, "Atom(not a \"comment\")");
            Assert.IsTrue(lCursor.SkipByte(cASCII.CR));
            Assert.IsTrue(lCursor.SkipByte(cASCII.LF));
            ZSkipQuotedString(lCursor, "At?Om");
            ZSkipQuotedString(lCursor, "At!om:{Atom}");
            ZSkipQuotedString(lCursor, "\tFWS beginning and end\t");
            Assert.IsFalse(lCursor.GetRFC822QuotedString(out _));
            Assert.AreEqual("\r\n\r\n", lCursor.GetRestAsString());

            lCursor = new cBytesCursor("   \t  \r\n Header    \tA");
            Assert.IsFalse(lCursor.GetRFC822QuotedString(out _));
            Assert.AreEqual(0, lCursor.Position.Byte);

            lCursor = new cBytesCursor("   \t  \r\n \"Header\"    \tA");
            ZSkipQuotedString(lCursor, "Header");
            Assert.AreEqual("A", lCursor.GetRestAsString());

        }

        private void ZSkipQuotedString(cBytesCursor pCursor, string pExpectedValue)
        {
            Assert.IsTrue(pCursor.GetRFC822QuotedString(out var lString));
            Assert.AreEqual(pExpectedValue, lString);
        }

        [TestMethod]
        public void cBytesCursor_RFC822_DomainLiteral()
        {
            var lCursor = new cBytesCursor("   \t (there is a domain\r\n literal coming up(and\tit'll\r\n\tbe a good one))  \r\n [Header]      \r\n ( now with with FWS ) [  \t  \r\n\t the.name.com  \r\n     ]    (now with embedded FWS)  [  \t  \r\n\t the \t   name   \r\n   com  \r\n     ]   (with quotes and utf8)    [     \\[   fr€d     ]     (something invalid)     [    [   ]   \r\n");
            ZSkipDomainLiteral(lCursor, "[Header]");
            ZSkipDomainLiteral(lCursor, "[the.name.com]");
            ZSkipDomainLiteral(lCursor, "[the name com]");
            ZSkipDomainLiteral(lCursor, "[[ fr€d]");
            Assert.IsFalse(lCursor.GetRFC822DomainLiteral(out _));
            Assert.AreEqual("[    [   ]   \r\n", lCursor.GetRestAsString());
        }

        private void ZSkipDomainLiteral(cBytesCursor pCursor, string pExpectedValue)
        {
            Assert.IsTrue(pCursor.GetRFC822DomainLiteral(out var lString));
            Assert.AreEqual(pExpectedValue, lString);
        }

        [TestMethod]
        public void cBytesCursor_RFC822_Domain()
        {
            var lCursor = new cBytesCursor("    \t   (first a dot-atom form)   fred.angus.bart   (now a obs form)    frxd  \t   .       angxs     .   \t  bxrt      (now a\r\n literal)     [    192.168.1.1     ]       \r\nNextHeader");

            ZSkipDomain(lCursor, "fred.angus.bart");
            ZSkipDomain(lCursor, "frxd.angxs.bxrt");
            ZSkipDomain(lCursor, "[192.168.1.1]");
            Assert.AreEqual("\r\nNextHeader", lCursor.GetRestAsString());
        }

        private void ZSkipDomain(cBytesCursor pCursor, string pExpectedValue)
        {
            Assert.IsTrue(pCursor.GetRFC822Domain(out var lString));
            Assert.AreEqual(pExpectedValue, lString);
        }

        [TestMethod]
        public void cBytesCursor_RFC822_LocalPart()
        {
            var lCursor = new cBytesCursor("    \t   (first a dot-atom form)   fred.angus.bart   (now a obs form)    frxd  \t   .       angxs     .   \t  bxrt      (now a\r\n quoted string)     \"th€ local part as a string\"       (then a second obsolete form)     \"fr€d\"  \t   .       angxs     .   \t  \"bzrt\"        \r\n ");
            ZSkipLocalPart(lCursor, "fred.angus.bart");
            ZSkipLocalPart(lCursor, "frxd.angxs.bxrt");
            ZSkipLocalPart(lCursor, "th€ local part as a string");
            ZSkipLocalPart(lCursor, "fr€d.angxs.bzrt");
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = new cBytesCursor("    \t   (edge case)   fred.angus.bart    .    []         \r\nNextHeader");
            ZSkipLocalPart(lCursor, "fred.angus.bart");
            Assert.AreEqual(".    []         \r\nNextHeader", lCursor.GetRestAsString());
        }

        private void ZSkipLocalPart(cBytesCursor pCursor, string pExpectedValue)
        {
            Assert.IsTrue(pCursor.GetRFC822LocalPart(out var lString));
            Assert.AreEqual(pExpectedValue, lString);
        }

        [TestMethod]
        public void cBytesCursor_RFC822_MessageId()
        {
            var lCursor = new cBytesCursor("     \r\n\t   (one)  <1234@local.machine.example>      <5678.21-Nov-1997@example.com>    <testabcd.1234@silly.example>     <1234   @   local(blah)  .machine .example>     <   \"5678.21-Nov-1997\" @ local . machine . example      >   <  \"5678.21-\\\"Nov\\\"-1997\" @ local . machine . example      >     ");

            ZSkipMessageId(lCursor, "<1234@local.machine.example>");
            ZSkipMessageId(lCursor, "<5678.21-Nov-1997@example.com>");
            ZSkipMessageId(lCursor, "<testabcd.1234@silly.example>");
            ZSkipMessageId(lCursor, "<1234@local.machine.example>");
            ZSkipMessageId(lCursor, "<5678.21-Nov-1997@local.machine.example>");
            ZSkipMessageId(lCursor, "<\"5678.21-\\\"Nov\\\"-1997\"@local.machine.example>");
        }

        private void ZSkipMessageId(cBytesCursor pCursor, string pExpectedValue)
        {
            Assert.IsTrue(pCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight));
            Assert.AreEqual(pExpectedValue, cMailTools.MessageId(lIdLeft, lIdRight));
        }
    }
}