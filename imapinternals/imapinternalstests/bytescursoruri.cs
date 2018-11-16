using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternalstests
{
    [TestClass]
    public class cBytesCursorURITests
    {
        [TestMethod]
        public void cBytesCursor_URITests()
        {
            var lContext = kTrace.Root.NewMethod(nameof(cBytesCursorURITests), nameof(cBytesCursor_URITests));

            sURIParts lParts;

            // 5

            lParts = ZParse("HTTP://user;AUTH=GSSAPI@SERVER2/", lContext);
            Assert.AreEqual("HTTP", lParts.Scheme);
            Assert.AreEqual("user;AUTH=GSSAPI", lParts.UserInfo);
            Assert.AreEqual("SERVER2", lParts.Host);
            Assert.IsTrue(lParts.HasParts(fURIParts.scheme | fURIParts.userinfo | fURIParts.host | fURIParts.pathroot));


            // 8
            //  € to type it hold alt and type 0128
            // 

            if (!LTryParse("IMAP://fr%E2%82%aCd@fred.com:123456789123456", out lParts)) throw new cTestsException("should have succeeded 8", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo | fParts.host | fParts.port) || lParts.UserInfo != "fr€d" || lParts.Port != "123456789123456") throw new cTestsException("unexpected state 8", lContext);





            // 9

            lCursor = new cBytesCursor("IMAP://user@[]:111");

            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 9", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response 9", lContext);
            if (cURL.TryParse(lString, out lURL)) throw new cTestsException("should have failed 9", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo) || lParts.Scheme != "IMAP" || lParts.UserInfo != "user" || lCursor.GetRestAsString() != "[]:111") throw new cTestsException("unexpected state 9", lContext);

            // 10

            lCursor = new cBytesCursor("IMAP://user@[1.2.3.4");

            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 10", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response 10", lContext);
            if (cURL.TryParse(lString, out lURL)) throw new cTestsException("should have failed 10", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo) || lParts.Scheme != "IMAP" || lParts.UserInfo != "user" || lCursor.GetRestAsString() != "[1.2.3.4") throw new cTestsException("unexpected state 9", lContext);

            // 12

            lCursor = new cBytesCursor("IMAP:///still here");

            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 12", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.pathroot | fParts.path) || lParts.Scheme != "IMAP" || lParts.Path != "still" || lCursor.GetRestAsString() != " here") throw new cTestsException("unexpected properties 12");
            if (cURL.TryParse(lString, out lURL)) throw new cTestsException("should have failed 12", lContext);

            // 13

            lCursor = new cBytesCursor("IMAP://:7still here");
            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 13", lContext);
            if (lCursor.GetRestAsString() != "still here") throw new cTestsException("should be some left 13", lContext);

            if (lParts.Port != "7") throw new cTestsException("unexpected properties in test 13");





            // 14
            lCursor = new cBytesCursor("IMAP://user;AUTH=*@SERVER2/REMOTE IMAP://user;AUTH=*@SERVER3/REMOTE]");
            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("2193.3.1");
            lURL = new cURL(lString);
            if (!lURL.IsMailboxReferral) throw new cTestsException("2193.3.2");
            if (!lCursor.SkipByte(cASCII.SPACE) || !lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("2193.3.3");
            lURI = new cURI(lString);
            if (!lURI.IsMailboxReferral) throw new cTestsException("2193.3.4");
            if (lCursor.Position.AtEnd) throw new cTestsException("2193.3.5");
            if (lCursor.GetRestAsString() != "]") throw new cTestsException("2193.3.6");
            if (lURI.MustUseAnonymous || lURI.UserId != "user" || lURI.MechanismName != null || lURI.Host != "SERVER3" || lURI.MailboxPath != "REMOTE") throw new cTestsException("2193.3.7");


            // 15

            if (!LTryParse("http://www.ics.uci.edu/pub/ietf/uri/#Related", out lParts)) throw new cTestsException("URI.15");

            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.pathroot | fParts.path | fParts.fragment)) throw new cTestsException("URI.15.1");
            if (lParts.Scheme != "http" || lParts.Host != "www.ics.uci.edu" || lParts.Path != "pub/ietf/uri/" || lParts.Fragment != "Related") throw new cTestsException("URI.15.2");

            // 16

            if (!LTryParse("http://www.ics.uci.edu/pub/ietf/uri/historical.html#WARNING", out lParts)) throw new cTestsException("URI.16");
            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.pathroot | fParts.path | fParts.fragment)) throw new cTestsException("URI.16.1");
            if (lParts.Scheme != "http" || lParts.Host != "www.ics.uci.edu" || lParts.Path != "pub/ietf/uri/historical.html" || lParts.Fragment != "WARNING") throw new cTestsException("URI.16.2");


            // 17 - IDN
            if (!LTryParse("IMAP://fr%E2%82%aCd@xn--frd-l50a.com:123456789123456", out lParts)) throw new cTestsException("should have succeeded 17", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo | fParts.host | fParts.port) || lParts.UserInfo != "fr€d" || lParts.Host != "xn--frd-l50a.com" || lParts.DisplayHost != "fr€d.com" || lParts.Port != "123456789123456") throw new cTestsException("unexpected state 8", lContext);


            // relative URIs
            //  TODO

            // edge cases
            //  TODO



        }

        private sURIParts ZParse(string pURI, cTrace.cContext pContext)
        {
            var lCursor = new cBytesCursor(pURI);
            Assert.IsTrue(lCursor.GetURI(out var lParts, out var lString, pContext));
            Assert.IsTrue(lCursor.Position.AtEnd);
            Assert.AreEqual(pURI, lString, false);
            return lParts;
        }
    }
}

