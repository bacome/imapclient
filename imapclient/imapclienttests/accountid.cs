using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;

namespace work.bacome.imapclienttests
{
    [TestClass]
    public class cAccountIdTests
    {
        [TestMethod]
        public void cAccountId_Tests()
        {
            cAccountId lFredFred1 = new cAccountId("xn--frd-l50a.com", "fred");
            cAccountId lFredFred2 = new cAccountId("fr€d.com", "fred");
            cAccountId lAngusFred = new cAccountId("angus.com", "fred");
            cAccountId lFredAngus = new cAccountId("xn--frd-l50a.com", "angus");
            cAccountId lFredAnon1 = new cAccountId("xn--frd-l50a.com", cSASLAnonymous.AnonymousCredentialId);
            cAccountId lFredAnon2 = new cAccountId("fr€d.com", cSASLAnonymous.AnonymousCredentialId);
            object lobj = new object();
            cAccountId lFredPreAuth1 = new cAccountId("xn--frd-l50a.com", lobj);
            cAccountId lFredPreAuth2 = new cAccountId("fr€d.com", lobj);
            cAccountId lFredPreAuth3 = new cAccountId("fr€d.com", new object());

            Assert.AreEqual(lFredFred1, lFredFred2);
            Assert.AreNotEqual(lFredFred1, lAngusFred);
            Assert.AreNotEqual(lFredFred1, lFredAngus);
            Assert.AreNotEqual(lFredFred1, lFredAnon1);
            Assert.AreNotEqual(lFredFred1, lFredPreAuth1);

            Assert.AreEqual(lFredAnon1, lFredAnon2);
            Assert.AreNotEqual(lFredAnon1, lFredPreAuth1);

            Assert.AreEqual(lFredPreAuth1, lFredPreAuth2);
            Assert.AreNotEqual(lFredPreAuth1, lFredPreAuth3);
        }
    }
}