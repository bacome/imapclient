using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cBytesCursor_URLParts
    {
        [TestMethod]
        public void cBytesCursor_URLParts_2221()
        {
            cURLParts lParts;

            // from rfc 2221

            lParts = ZParse("IMAP://MIKE@SERVER2/");
            Assert.IsTrue(lParts.IsHomeServerReferral);
            lParts = ZParse("IMAP://user;AUTH=GSSAPI@SERVER2/");
            Assert.IsTrue(lParts.IsHomeServerReferral);
            lParts = ZParse("IMAP://user;AUTH=*@SERVER2/");
            Assert.IsTrue(lParts.IsHomeServerReferral);
        }

        [TestMethod]
        public void cBytesCursor_URLParts_2193()
        {
            cURLParts lParts;

            // from rfc 2193

            lParts = ZParse("IMAP://user;AUTH=*@SERVER2/SHARED/FOO");
            Assert.IsTrue(lParts.IsMailboxReferral);
            Assert.AreEqual("SHARED/FOO", lParts.MailboxPath, false);
        }

        [TestMethod]
        public void cBytesCursor_URLParts_5092()
        {
            cURLParts lParts;

            // from rfc 5092

            lParts = ZParse("imap://minbari.example.org/gray-council;UIDVALIDITY=385759045/;UID=20/;PARTIAL=0.1024");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsTrue(lParts.HasParts(fURLParts.scheme | fURLParts.host | fURLParts.mailboxname | fURLParts.uidvalidity | fURLParts.uid | fURLParts.partial | fURLParts.partiallength));
            Assert.IsTrue(lParts.MustUseAnonymous);
            Assert.AreEqual("minbari.example.org", lParts.Host);
            Assert.AreEqual("gray-council", lParts.MailboxPath);
            Assert.AreEqual(385759045u, lParts.UIDValidity.Value);
            Assert.AreEqual(20u, lParts.UID.Value);
            Assert.AreEqual(0u, lParts.PartialOffset.Value);
            Assert.AreEqual(1024u, lParts.PartialLength.Value);

            lParts = ZParse("imap://psicorp.example.org/~peter/%E6%97%A5%E6%9C%AC%E8%AA%9E/%E5%8F%B0%E5%8C%97");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsTrue(lParts.IsMailboxReferral);
            Assert.IsTrue(lParts.HasParts(fURLParts.scheme | fURLParts.host | fURLParts.mailboxname));
            Assert.IsTrue(lParts.MustUseAnonymous);
            Assert.AreEqual("psicorp.example.org", lParts.Host);
            Assert.AreEqual("~peter/日本語/台北", lParts.MailboxPath);

            lParts = ZParse("imap://;AUTH=GSSAPI@minbari.example.org/gray-council/;uid=20/;section=1.2");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsTrue(lParts.HasParts(fURLParts.scheme | fURLParts.mechanismname | fURLParts.host | fURLParts.mailboxname | fURLParts.uid | fURLParts.section));
            Assert.IsFalse(lParts.MustUseAnonymous);
            Assert.AreEqual("GSSAPI", lParts.MechanismName);
            Assert.AreEqual("minbari.example.org", lParts.Host);
            Assert.AreEqual("gray-council", lParts.MailboxPath);
            Assert.AreEqual(20u, lParts.UID.Value);
            Assert.AreEqual("1.2", lParts.Section);

            lParts = ZParse("imap://;AUTH=*@minbari.example.org/gray%20council?SUBJECT%20shadows");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsTrue(lParts.HasParts(fURLParts.scheme | fURLParts.mechanismname | fURLParts.host | fURLParts.mailboxname | fURLParts.search));
            Assert.IsFalse(lParts.MustUseAnonymous);
            Assert.IsNull(lParts.MechanismName);
            Assert.AreEqual("minbari.example.org", lParts.Host);
            Assert.AreEqual("gray council", lParts.MailboxPath);
            Assert.AreEqual("SUBJECT shadows", lParts.Search);

            lParts = ZParse("imap://john;AUTH=*@minbari.example.org/babylon5/personel?charset%20UTF-8%20SUBJECT%20%7B14+%7D%0D%0A%D0%98%D0%B2%D0%B0%D0%BD%D0%BE%D0%B2%D0%B0");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsTrue(lParts.HasParts(fURLParts.scheme | fURLParts.userid | fURLParts.mechanismname | fURLParts.host | fURLParts.mailboxname | fURLParts.search));
            Assert.IsFalse(lParts.MustUseAnonymous);
            Assert.AreEqual("john", lParts.UserId);
            Assert.IsNull(lParts.MechanismName);
            Assert.AreEqual("minbari.example.org", lParts.Host);
            Assert.AreEqual("babylon5/personel", lParts.MailboxPath);
            Assert.AreEqual("charset UTF-8 SUBJECT {14+}\r\nИванова", lParts.Search);
        }

        [TestMethod]
        public void cBytesCursor_URLParts_4467()
        {
            cURLParts lParts;

            // URLAUTH - rfc 4467

            lParts = ZParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsFalse(lParts.IsAuthorisable);
            Assert.IsFalse(lParts.IsAuthorised);

            lParts = ZParse("imap://example.com/Shared/;uid=20/;section=1.2;urlauth=submit+fred");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsFalse(lParts.IsAuthorisable);
            Assert.IsFalse(lParts.IsAuthorised);

            lParts = ZParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2;urlauth=submit+fred");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsTrue(lParts.IsAuthorisable);
            Assert.IsFalse(lParts.IsAuthorised);

            lParts = ZParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2;urlauth=submit+fred:internal:91354a473744909de610943775f92038");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsFalse(lParts.IsAuthorisable);
            Assert.IsTrue(lParts.IsAuthorised);

            lParts = ZParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2;expire=2018-11-20T22:23:24Z;urlauth=submit+fred:internal:91354a473744909de610943775f92038");
            Assert.IsFalse(lParts.IsHomeServerReferral);
            Assert.IsFalse(lParts.IsMailboxReferral);
            Assert.IsFalse(lParts.IsAuthorisable);
            Assert.IsTrue(lParts.IsAuthorised);
            Assert.AreEqual(new DateTime(2018, 11, 20, 22, 23, 24, DateTimeKind.Utc), lParts.Expire.UtcDateTime);
        }

        [TestMethod]
        public void cBytesCursor_URLParts_IDN()
        {
            cURLParts lParts;

            lParts = ZParse("imap://fr%E2%82%aCd@xn--frd-l50a.com/INBOX/;uid=20/;section=1.2");
            Assert.AreEqual("fr€d", lParts.UserId);
            Assert.AreEqual("xn--frd-l50a.com", lParts.Host);
            Assert.AreEqual("fr€d.com", lParts.DisplayHost);

            lParts = ZParse("imap://fr%E2%82%aCd@fr%E2%82%aCd.com/INBOX/;uid=20/;section=1.2");
            Assert.AreEqual("fr€d", lParts.UserId);
            Assert.AreEqual("fr€d.com", lParts.Host);
            Assert.AreEqual("fr€d.com", lParts.DisplayHost);
        }

        [TestMethod]
        public void cBytesCursor_URLParts_EDGES()
        {
            cBytesCursor lCursor;
            cURLParts lParts;

            lCursor = new cBytesCursor("IMAP://user;AUTH=*@SERVER2/REMOTE IMAP://user;AUTH=*@SERVER3/REMOTE");
            Assert.IsTrue(lCursor.ProcessURLParts(out lParts));
            Assert.IsFalse(lCursor.Position.AtEnd);
            Assert.AreEqual(" IMAP://user;AUTH=*@SERVER3/REMOTE", lCursor.GetRestAsString());


            lCursor = new cBytesCursor("IMAP://user@[]:111");
            Assert.IsFalse(lCursor.ProcessURLParts(out lParts));

            lCursor = new cBytesCursor("IMAP://user@[1]:111");
            Assert.IsTrue(lCursor.ProcessURLParts(out lParts));
            Assert.AreEqual(111, lParts.Port);

            lCursor = new cBytesCursor("IMAP://user@[1.2.3.4");
            Assert.IsFalse(lCursor.ProcessURLParts(out lParts));

            lCursor = new cBytesCursor("IMAP://user@[1.2.3.4]");
            Assert.IsTrue(lCursor.ProcessURLParts(out lParts));
            Assert.AreEqual(143, lParts.Port);



            // network-path
            //  TODO

            // absolute-path
            //  TODO

            // edge cases for the URL
            //  TODO
        }

        [TestMethod]
        public void cBytesCursor_URLParts_TODO()
        {
            var lContext = kTrace.Root.NewMethod(nameof(Test_cBytesCursor_URLParts), nameof(cBytesCursor_URLParts_TODO));
            lContext.TraceError("still some tests to write");

            // network-path
            //  TODO

            // absolute-path
            //  TODO

            // edge cases for the URL
            //  TODO
        }

        private cURLParts ZParse(string pURL)
        {
            var lCursor = new cBytesCursor(pURL);
            Assert.IsTrue(lCursor.ProcessURLParts(out var lParts));
            Assert.IsTrue(lCursor.Position.AtEnd);
            return lParts;
        }
    }
}