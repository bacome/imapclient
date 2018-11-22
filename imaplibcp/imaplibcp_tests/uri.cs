using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;

namespace work.bacome.imapclienttests
{
    [TestClass]
    public class Test_cURI
    {
        [TestMethod]
        public void cURI_Tests()
        {
            var lURI = new cURI("IMAP://user;AUTH=*@SERVER2/REMOTE");
            Assert.IsNotNull(lURI.URL);
            Assert.IsTrue(lURI.IsMailboxReferral);
            Assert.IsFalse(lURI.MustUseAnonymous);
            Assert.AreEqual("IMAP", lURI.Scheme);
            Assert.AreEqual("user", lURI.UserId);
            Assert.AreEqual("user;AUTH=*", lURI.UserInfo);
            Assert.AreEqual("SERVER2", lURI.Host);
            Assert.AreEqual(143, lURI.Port);
            Assert.IsNull(lURI.PortString);
            Assert.AreEqual("REMOTE", lURI.MailboxPath);
            Assert.AreEqual("REMOTE", lURI.Path);

            lURI = new cURI("http://user;AUTH=*@SERVER2/REMOTE");
            Assert.IsNull(lURI.URL);
            Assert.IsFalse(lURI.IsMailboxReferral);
            Assert.IsFalse(lURI.MustUseAnonymous);
            Assert.AreEqual("http", lURI.Scheme);
            Assert.IsNull(lURI.UserId);
            Assert.AreEqual("user;AUTH=*", lURI.UserInfo);
            Assert.AreEqual("SERVER2", lURI.Host);
            Assert.AreEqual(-1, lURI.Port);
            Assert.IsNull(lURI.PortString);
            Assert.IsNull(lURI.MailboxPath);
            Assert.AreEqual("REMOTE", lURI.Path);
        }
    }
}