using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;

namespace work.bacome.imapclienttests
{
    [TestClass]
    public class cURITests
    {
        [TestMethod]
        public void cURI_Tests()
        {
            var lURI = new cURI("IMAP://user;AUTH=*@SERVER2/REMOTE");
            Assert.IsTrue(lURI.IsMailboxReferral);
            Assert.IsFalse(lURI.MustUseAnonymous);
            Assert.AreEqual("IMAP", lURI.Scheme);
            Assert.AreEqual("user", lURI.UserId);
            Assert.AreEqual("user;AUTH=*", lURI.UserInfo);
            Assert.AreEqual("SERVER2", lURI.Host);
            Assert.AreEqual("REMOTE", lURI.MailboxPath);
        }
    }
}