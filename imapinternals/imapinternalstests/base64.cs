using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapinternalstests
{
    [TestClass]
    public class cBase64Tests
    {
        [TestMethod]
        public void cBase64_Tests()
        {
            ZCheck(
                "Man is distinguished, not only by his reason, but by this singular passion from other animals, which is a lust of the mind, that by a perseverance of delight in the continued and indefatigable generation of knowledge, exceeds the short vehemence of any carnal pleasure.",
                "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCBieSB0aGlzIHNpbmd1bGFyIHBhc3Npb24gZnJvbSBvdGhlciBhbmltYWxzLCB3aGljaCBpcyBhIGx1c3Qgb2YgdGhlIG1pbmQsIHRoYXQgYnkgYSBwZXJzZXZlcmFuY2Ugb2YgZGVsaWdodCBpbiB0aGUgY29udGludWVkIGFuZCBpbmRlZmF0aWdhYmxlIGdlbmVyYXRpb24gb2Yga25vd2xlZGdlLCBleGNlZWRzIHRoZSBzaG9ydCB2ZWhlbWVuY2Ugb2YgYW55IGNhcm5hbCBwbGVhc3VyZS4="
                );

            ZCheck("pleasure.", "cGxlYXN1cmUu");
            ZCheck("leasure.", "bGVhc3VyZS4=");
            ZCheck("easure.", "ZWFzdXJlLg==");
            ZCheck("asure.", "YXN1cmUu");
            ZCheck("sure.", "c3VyZS4=");
        }


        private void ZCheck(string pFrom, string pExpected)
        {
            var lFrom = new cBytes(pFrom);
            var lExpected = new cBytes(pExpected);
            var lTo = cBase64.Encode(lFrom);

            Assert.IsTrue(cBase64.TryDecode(lTo, out var lReturn, out var lError), $"decode fail: {lError}");
            Assert.IsTrue(cASCII.Compare(lFrom, lReturn, true), $"round trip fail {lReturn} vs expected {lFrom}");
            Assert.IsTrue(cASCII.Compare(lTo, lExpected, true), $"expected value fail {lTo} vs expected {lExpected}");
        }
    }
}