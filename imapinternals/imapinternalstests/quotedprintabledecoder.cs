using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapinternalstests
{
    [TestClass]
    public class cQuotedPrintableDecoderTests
    {
        [TestMethod]
        public void cQuotedPrintableDecoder()
        {
            Assert.AreEqual("Now's the time for all folk to come to the aid of their country.\r\n", ZTest("testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t\r\n"), "1");
            Assert.AreEqual("Now's the time for all folk to come to the aid of their country.", ZTest("testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t"), "2");
        }

        private string ZTest(params string[] pLines)
        {
            using (var lStream = new MemoryStream())
            {
                iTransformer lTransformer = new cQuotedPrintableDecoder();

                int lOffset = 4;
                byte[] lBuffer;

                foreach (var lLine in pLines)
                {
                    var lBytes = new cBytes(lLine);
                    lBuffer = lTransformer.Transform(lBytes, lOffset, lBytes.Count - lOffset);
                    lStream.Write(lBuffer, 0, lBuffer.Length);
                    lOffset = 0;
                }

                lBuffer = lTransformer.Transform(cBytes.Empty, 0, 0);
                lStream.Write(lBuffer, 0, lBuffer.Length);

                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }
    }
}
