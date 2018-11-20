using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cBytesCursor_Base
    {
        [TestMethod]
        public void cBytesCursor_Base_Tests()
        {
            var lCursor = ZMakeCursor("", "", "");
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("", "", "{ ", "");
            Assert.IsFalse(lCursor.Position.AtEnd);
            Assert.IsFalse(lCursor.SkipByte(cASCII.SPACE));
            ZSkipString(lCursor, " ");
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("", "", " ", "");
            Assert.IsFalse(lCursor.Position.AtEnd);
            Assert.IsTrue(lCursor.SkipByte(cASCII.SPACE));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("", "AB", "CD", "");
            var lBookmark = lCursor.Position;
            Assert.IsFalse(lCursor.SkipBytes(new cBytes("ABCDE")));
            Assert.IsFalse(lCursor.SkipBytes(new cBytes("ABcd"), true));
            Assert.IsTrue(lCursor.SkipBytes(new cBytes("ABcd")));
            Assert.IsTrue(lCursor.Position.AtEnd);
            lCursor.Position = lBookmark;
            Assert.IsTrue(lCursor.SkipBytes(new cBytes("ABCD")));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("ABCD", "{ABCD", "");
            lBookmark = lCursor.Position;
            Assert.IsTrue(lCursor.SkipByte(cASCII.A));
            lCursor.Position = lBookmark;
            Assert.IsFalse(lCursor.SkipByte(cASCII.a, true));
            Assert.IsTrue(lCursor.SkipByte(cASCII.a));
            Assert.IsTrue(lCursor.SkipByte(cASCII.B));
            lCursor.Position = lBookmark;
            Assert.IsFalse(lCursor.SkipBytes(new cBytes("ABX")));
            Assert.IsFalse(lCursor.SkipBytes(new cBytes("ABCDA")));
            Assert.IsTrue(lCursor.SkipBytes(new cBytes("AB")));
            Assert.IsTrue(lCursor.SkipBytes(new cBytes("CD")));
            Assert.IsFalse(lCursor.SkipBytes(null));
            Assert.IsFalse(lCursor.SkipByte(cASCII.A));
            lBookmark = lCursor.Position;
            ZSkipString(lCursor, "ABCD");
            Assert.IsTrue(lCursor.Position.AtEnd);
            Assert.IsFalse(lCursor.GetString(out string _));

            lCursor = ZMakeCursor("A", "B");
            Assert.IsTrue(lCursor.SkipByte(cASCII.A));
            Assert.IsTrue(lCursor.SkipByte(cASCII.B));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("", "A", "", "B");
            Assert.IsTrue(lCursor.SkipByte(cASCII.A));
            Assert.IsTrue(lCursor.SkipByte(cASCII.B));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("\"AB\\\\CD\\\"EF\" ");
            ZSkipString(lCursor, "AB\\CD\"EF");
            Assert.IsFalse(lCursor.Position.AtEnd);
            Assert.IsFalse(lCursor.GetString(out string _));
            Assert.IsTrue(lCursor.SkipByte(cASCII.SPACE));
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("\"AB\\\\CD\\\"EF");
            Assert.IsFalse(lCursor.GetString(out string _));
            Assert.IsTrue(lCursor.SkipByte(cASCII.DQUOTE));
            Assert.IsTrue(lCursor.SkipByte(cASCII.a));
            Assert.AreEqual("B\\\\CD\\\"EF", lCursor.GetRestAsString());
            Assert.IsTrue(lCursor.Position.AtEnd);

            lCursor = ZMakeCursor("AT", "OM]ATOM(", "{literal", "ATOM)ATOM{ATOM%ATOM*ATOM\"ATOM\\ATOM ASTRIN]G");
            lBookmark = lCursor.Position;
            cByteList lToken;

            Assert.IsTrue(lCursor.GetToken(cCharset.Atom, null, null, out lToken));
            Assert.IsTrue(cASCII.Compare(lToken, new cBytes("aToM"), false));
            Assert.IsTrue(lCursor.SkipByte(cASCII.RBRACKET));

            lCursor.Position = lBookmark;
            Assert.IsTrue(lCursor.GetToken(cCharset.AString, null, null, out lToken));
            Assert.IsTrue(cASCII.Compare(lToken, new cBytes("aToM]atom"), false));
            Assert.IsTrue(lCursor.SkipByte(cASCII.LPAREN));

            Assert.IsFalse(lCursor.GetToken(cCharset.All, null, null, out lToken));
            ZSkipString(lCursor, "literal");

            Assert.IsFalse(lCursor.GetToken(cCharset.Atom, null, null, out lToken, 5));

            Assert.IsTrue(lCursor.GetToken(cCharset.Atom, null, null, out lToken, 1, 2));
            Assert.IsTrue(cASCII.Compare(lToken, new cBytes("AT"), false));

            lCursor = ZMakeCursor("fr%E2%82%acd");
            lBookmark = lCursor.Position;
            string lString;

            Assert.IsTrue(lCursor.GetToken(cCharset.TextNotRBRACKET, null, null, out lString));
            Assert.AreEqual("fr%E2%82%acd", lString);
        
            lCursor.Position = lBookmark;
            Assert.IsTrue(lCursor.GetToken(cCharset.TextNotRBRACKET, cASCII.PERCENT, null, out lString));
            Assert.AreEqual("fr€d", lString);

            lCursor = ZMakeCursor("A1", "2345", "", "67890123");
            uint lNumber;

            Assert.IsFalse(lCursor.GetNumber(out _, out _));
            Assert.IsTrue(lCursor.SkipByte(cASCII.A));
            Assert.IsFalse(lCursor.GetNumber(out _, out _));
            Assert.IsTrue(lCursor.GetNumber(out _, out lNumber, 1, 9));
            Assert.AreEqual(123456789u, lNumber);
            Assert.IsFalse(lCursor.GetNZNumber(out _, out _));
            Assert.IsTrue(lCursor.SkipByte(cASCII.ZERO));
            Assert.IsTrue(lCursor.GetNZNumber(out _, out lNumber));
            Assert.AreEqual(123u, lNumber);
        }

        private void ZSkipString(cBytesCursor pCursor, string pExpectedValue)
        {
            var lBookmark = pCursor.Position;
            Assert.IsTrue(pCursor.GetString(out string lString));
            Assert.AreEqual(pExpectedValue, lString);
            pCursor.Position = lBookmark;
            Assert.IsTrue(pCursor.GetString(out IList<byte> lBytes));
            Assert.AreEqual(pExpectedValue, cTools.UTF8BytesToString(lBytes));
        }

        private cBytesCursor ZMakeCursor(params string[] pLines)
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