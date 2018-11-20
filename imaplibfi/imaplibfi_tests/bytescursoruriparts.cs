using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cBytesCursor_URIParts
    {
        [TestMethod]
        public void cBytesCursor_URIParts_Tests()
        {
            cBytesCursor lCursor;
            cURIParts lParts;

            // 5

            lParts = ZParse("HTTP://user;AUTH=GSSAPI@SERVER2/");
            Assert.AreEqual("HTTP", lParts.Scheme);
            Assert.AreEqual("user;AUTH=GSSAPI", lParts.UserInfo);
            Assert.AreEqual("SERVER2", lParts.Host);
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.userinfo | fURIParts.host | fURIParts.pathroot));

            // 8
            //  € to type it hold alt and type 0128
            // 

            lParts = ZParse("IMAP://fr%E2%82%aCd@fred.com:123456789123456");
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.userinfo | fURIParts.host | fURIParts.port));
            Assert.AreEqual("fr€d", lParts.UserInfo);
            Assert.AreEqual("123456789123456", lParts.Port);

            // 9

            lCursor = new cBytesCursor("IMAP://user@[]:111");
            Assert.IsTrue(lCursor.ProcessURIParts(out lParts));
            Assert.IsFalse(lCursor.Position.AtEnd);
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.userinfo));
            Assert.AreEqual("IMAP", lParts.Scheme);
            Assert.AreEqual("user", lParts.UserInfo);
            Assert.AreEqual("[]:111", lCursor.GetRestAsString());

            lParts = ZParse("IMAP://user@[1]:111");
            Assert.AreEqual("[1]", lParts.Host);
            Assert.AreEqual("111", lParts.Port);

            // 10

            lCursor = new cBytesCursor("IMAP://user@[1.2.3.4");
            Assert.IsTrue(lCursor.ProcessURIParts(out lParts));
            Assert.IsFalse(lCursor.Position.AtEnd);
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.userinfo));
            Assert.AreEqual("IMAP", lParts.Scheme);
            Assert.AreEqual("user", lParts.UserInfo);
            Assert.AreEqual("[1.2.3.4", lCursor.GetRestAsString());

            lParts = ZParse("IMAP://user@[1.2.3.4]");
            Assert.AreEqual("[1.2.3.4]", lParts.Host);
            Assert.IsNull(lParts.Port);

            // 12

            lCursor = new cBytesCursor("IMAP:///still here");
            Assert.IsTrue(lCursor.ProcessURIParts(out lParts));
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.pathroot | fURIParts.path));
            Assert.AreEqual("IMAP", lParts.Scheme);
            Assert.AreEqual("still", lParts.Path);
            Assert.AreEqual(" here", lCursor.GetRestAsString());

            // 13

            lCursor = new cBytesCursor("IMAP://:7still here");
            Assert.IsTrue(lCursor.ProcessURIParts(out lParts));
            Assert.AreEqual("7", lParts.Port);
            Assert.AreEqual("still here", lCursor.GetRestAsString());

            // 14
            lCursor = new cBytesCursor("IMAP://user;AUTH=*@SERVER2/REMOTE IMAP://user;AUTH=*@SERVER3/REMOTE]");
            Assert.IsTrue(lCursor.ProcessURIParts(out lParts) && lCursor.SkipByte(cASCII.SPACE) && lCursor.ProcessURIParts(out lParts) && lCursor.SkipByte(cASCII.RBRACKET) && lCursor.Position.AtEnd);


            // 15

            lParts = ZParse("http://www.ics.uci.edu/pub/ietf/uri/#Related");
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.host | fURIParts.pathroot | fURIParts.path | fURIParts.fragment));
            Assert.AreEqual("http", lParts.Scheme);
            Assert.AreEqual("www.ics.uci.edu", lParts.Host);
            Assert.AreEqual("pub/ietf/uri/", lParts.Path);
            Assert.AreEqual("Related", lParts.Fragment);

            // 16

            lParts = ZParse("http://www.ics.uci.edu/pub/ietf/uri/historical.html#WARNING");
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.host | fURIParts.pathroot | fURIParts.path | fURIParts.fragment));
            Assert.AreEqual("http", lParts.Scheme);
            Assert.AreEqual("www.ics.uci.edu", lParts.Host);
            Assert.AreEqual("pub/ietf/uri/historical.html", lParts.Path);
            Assert.AreEqual("WARNING", lParts.Fragment);

            // 17 - IDN

            lParts = ZParse("IMAP://fr%E2%82%aCd@xn--frd-l50a.com:123456789123456");
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.userinfo | fURIParts.host | fURIParts.port));
            Assert.AreEqual("fr€d", lParts.UserInfo);
            Assert.AreEqual("xn--frd-l50a.com", lParts.Host);
            Assert.AreEqual("fr€d.com", lParts.DisplayHost);
            Assert.AreEqual("123456789123456", lParts.Port);
        }

        [TestMethod]
        public void cBytesCursor_URIParts_TODO()
        {
            var lContext = kTrace.Root.NewMethod(nameof(Test_cBytesCursor_URIParts), nameof(cBytesCursor_URIParts_TODO));
            lContext.TraceError("still some tests to write");

            // relative URIs
            //  TODO

            // edge cases
            //  TODO
        }

        private cURIParts ZParse(string pURI)
        {
            var lCursor = new cBytesCursor(pURI);
            Assert.IsTrue(lCursor.ProcessURIParts(out var lParts));
            Assert.IsTrue(lCursor.Position.AtEnd);
            return lParts;
        }
    }
}

