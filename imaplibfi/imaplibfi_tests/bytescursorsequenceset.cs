using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cBytesCursor_SequenceSet
    {
        [TestMethod]
        public void cBytesCursor_SequenceSet_Tests()
        {
            ZParse("*", true, "*", "");
            ZParse("*", false, "", "*");

            ZParse("*:7", true, "7:*", "");
            ZParse("*:7", false, "", "*:7");

            ZParse("2,1,4:7,9:*", true, "2,1,4:7,9:*", "");
            ZParse("2,1,4:7,9:*", false, "2,1,4:7,9", ":*");

            ZParse("0,1,4:7,9:*", true, "", "0,1,4:7,9:*");

            ZParse("2,1,7:4,9:*", false, "2,1,4:7,9", ":*");
        }

        private void ZParse(string pInput, bool pAsteriskAllowed, string pExtracted, string pLeft)
        {
            var lCursor = new cBytesCursor(pInput);

            if (pExtracted == "") Assert.IsFalse(lCursor.GetSequenceSet(pAsteriskAllowed, out var lSeqSet));
            else
            {
                Assert.IsTrue(lCursor.GetSequenceSet(pAsteriskAllowed, out var lSeqSet));
                Assert.AreEqual(pExtracted, lSeqSet.ToCompactString());
            }

            Assert.AreEqual(pLeft, lCursor.GetRestAsString());
        }
    }
}
