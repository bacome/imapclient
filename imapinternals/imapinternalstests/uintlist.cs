using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;

namespace work.bacome.imapinternalstests
{
    [TestClass]
    public class cUIntListTests
    {
        [TestMethod]
        public void cUIntList_Tests()
        {
            ZTest(new cSequenceSet[] { new cSequenceSet(4) }, 0, new uint[] { 4 });
            ZTest(new cSequenceSet[] { new cSequenceSet(4, 9) }, 0, new uint[] { 4, 5, 6, 7, 8, 9 });
            ZTest(new cSequenceSet[] { new cSequenceSet(4, 9), new cSequenceSet(4, 9) }, 0, new uint[] { 4, 5, 6, 7, 8, 9 });
            ZTest(new cSequenceSet[] { new cSequenceSet(4, 9), new cSequenceSet(3, 5) }, 0, new uint[] { 3, 4, 5, 6, 7, 8, 9 });
            ZTest(new cSequenceSet[] { new cSequenceSet(3, 5) }, 0, new uint[] { 3, 4, 5 });

            var l5toAsterisk = new cSequenceSet(new cSequenceSetItem[] { new cSequenceSetRange(new cSequenceSetNumber(5), cSequenceSetItem.Asterisk) });
            var lAsteriskTo5 = new cSequenceSet(new cSequenceSetItem[] { new cSequenceSetRange(cSequenceSetItem.Asterisk, new cSequenceSetNumber(5)) });

            Assert.AreEqual(l5toAsterisk.ToString(), lAsteriskTo5.ToString(), false);

            var lAsteriskToAsterisk = new cSequenceSet(new cSequenceSetItem[] { new cSequenceSetRange(cSequenceSetItem.Asterisk, cSequenceSetItem.Asterisk) });

            ZTest(new cSequenceSet[] { l5toAsterisk }, 0, null);
            ZTest(new cSequenceSet[] { l5toAsterisk }, 3, new uint[] { 3, 4, 5 });
            ZTest(new cSequenceSet[] { l5toAsterisk }, 7, new uint[] { 5, 6, 7 });
            ZTest(new cSequenceSet[] { lAsteriskToAsterisk }, 7, new uint[] { 7 });
            ZTest(new cSequenceSet[] { lAsteriskToAsterisk }, 9, new uint[] { 9 });
            ZTest(new cSequenceSet[] { l5toAsterisk, lAsteriskToAsterisk }, 7, new uint[] { 5, 6, 7 });
        }

        private void ZTest(IEnumerable<cSequenceSet> pSequenceSets, uint pAsterisk, IEnumerable<uint> pUInts)
        {
            if (cUIntList.TryConstruct(pSequenceSets, pAsterisk, true, out var lUInts))
            {
                Assert.IsNotNull(lUInts);
                Assert.IsNotNull(pUInts);
                Assert.AreEqual(pUInts.Count(), lUInts.Count);
                Assert.AreEqual(0, lUInts.Except(pUInts).Count());
            }
            else Assert.IsNull(pUInts);
        }
    }
}