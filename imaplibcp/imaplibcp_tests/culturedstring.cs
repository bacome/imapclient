using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapclienttests
{
    [TestClass]
    public class Test_CulturedString
    {
        [TestMethod]
        public void cCulturedString_Tests()
        {
            ZTest("=?iso-8859-1?q?this=20is=20some=20text?=", "this is some text");
            ZTest("=?iso-8859-1?q?this=20is=20some=20text?= ", "this is some text");
            ZTest("=?iso-8859-1?q?this=20is=20some=20text?= a", "this is some text a");
            ZTest("=?iso-8859-1?q?this=20is=20some=20text?=  a", "this is some text  a");
            ZTest("=?iso-8859-1?q?this=20is=20some=20text?=a  a", "=?iso-8859-1?q?this=20is=20some=20text?=a  a");
            ZTest("=?US-ASCII?Q?Keith_Moore?= <moore@cs.utk.edu>", "Keith Moore <moore@cs.utk.edu>");
            ZTest("=?ISO-8859-1?Q?Keld_J=F8rn_Simonsen?= <keld@dkuug.dk>", "Keld Jørn Simonsen <keld@dkuug.dk>");
            ZTest("=?ISO-8859-1?Q?Andr=E9?= Pirard <PIRARD@vm1.ulg.ac.be>", "André Pirard <PIRARD@vm1.ulg.ac.be>");
            ZTest("=?ISO-8859-1?B?SWYgeW91IGNhbiByZWFkIHRoaXMgeW8=?= =?ISO-8859-2?B?dSB1bmRlcnN0YW5kIHRoZSBleGFtcGxlLg==?=", "If you can read this you understand the example.");
            ZTest("=?ISO-8859-1?Q?a?= b", "a b");
            ZTest("=?ISO-8859-1?Q?a?= =?ISO-8859-1?Q?b?=", "ab");
            ZTest("=?ISO-8859-1?Q?a?=  =?ISO-8859-1?Q?b?=", "ab");
            ZTest("=?ISO-8859-1?Q?a_b?=", "a b");
            ZTest("=?ISO-8859-1?Q?a?= =?ISO-8859-2?Q?_b?=", "a b");

            var lCS = new cCulturedString(new cBytes("=?US-ASCII?Q?Keith_Moore?= <moore@cs.utk.edu>"));
            Assert.AreEqual("Keith Moore <moore@cs.utk.edu>", lCS.ToString());
            Assert.IsNull(lCS.Parts[0].LanguageTag);

            lCS = new cCulturedString(new cBytes("=?US-ASCII*EN?Q?Keith_Moore?= <moore@cs.utk.edu>"));
            Assert.AreEqual("Keith Moore <moore@cs.utk.edu>", lCS.ToString());
            Assert.AreEqual("EN", lCS.Parts[0].LanguageTag);
        }

        private void ZTest(string pInput, string pExpectedResult)
        {
            Assert.AreEqual(pExpectedResult, new cCulturedString(new cBytes(pInput)).ToString());
        }
    }
}
