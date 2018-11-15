using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace imapclienttests
{
    [TestClass]
    public class cBytesCursorTests
    {
        [TestMethod]
        public void BytesCursor_SequenceSet()
        {
            // parsing and construction tests
            _Tests_1("*", "cSequenceSet(cAsterisk())", null, new cUIntList(new uint[] { 15 }), "cSequenceSet(cSequenceSetNumber(15))");
            _Tests_1("0", null, "0", null, null);
            _Tests_1("2,4:7,9,12:*", "cSequenceSet(cSequenceSetNumber(2),cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(7)),cSequenceSetNumber(9),cSequenceSetRange(cSequenceSetNumber(12),cAsterisk()))", null, new cUIntList(new uint[] { 2, 4, 5, 6, 7, 9, 12, 13, 14, 15 }), "cSequenceSet(cSequenceSetNumber(2),cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(7)),cSequenceSetNumber(9),cSequenceSetRange(cSequenceSetNumber(12),cSequenceSetNumber(15)))");
            _Tests_1("*:4,7:5", "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(4),cAsterisk()),cSequenceSetRange(cSequenceSetNumber(5),cSequenceSetNumber(7)))", null, new cUIntList(new uint[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }), "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(15)))");
        }

        private void _Tests_1(string pCursor, string pExpSeqSet, string pExpRemainder, cUIntList pExpList, string pExpSeqSet2)
        {
            var lCursor = new cBytesCursor(pCursor);

            if (lCursor.GetSequenceSet(true, out var lSequenceSet))
            {
                string lSeqSet = lSequenceSet.ToString();
                if (lSeqSet != pExpSeqSet) throw new cTestsException($"failed to get expected sequence set from {pCursor}: got '{lSeqSet}' vs expected '{pExpSeqSet}'");

                if (!cUIntList.TryConstruct(lSequenceSet, 15, true, out var lTemp)) throw new cTestsException($"failed to get an uintlist from {lSequenceSet}");
                if (pExpList.Count != lTemp.Count) throw new cTestsException($"failed to get expected uintlist from {lSequenceSet}");
                var lList = new cUIntList(lTemp.OrderBy(i => i));
                for (int i = 0; i < pExpList.Count; i++) if (pExpList[i] != lList[i]) throw new cTestsException($"failed to get expected uintlist from {lSequenceSet}");

                string lSeqSet2 = cSequenceSet.FromUInts(lList).ToString();
                if (lSeqSet2 != pExpSeqSet2) throw new cTestsException($"failed to get expected sequence set from {lList}: got '{lSeqSet2}' vs expected '{pExpSeqSet2}'");
            }
            else if (pExpSeqSet != null) throw new cTestsException($"failed to get a sequence set from {pCursor}");


            if (lCursor.Position.AtEnd)
            {
                if (pExpRemainder == null) return;
                throw new cTestsException($"expected a remainder from {pCursor}");
            }

            string lRemainder = lCursor.GetRestAsString();
            if (lRemainder != pExpRemainder) throw new cTestsException($"failed to get expected remainder set from {pCursor}: '{lRemainder}' vs '{pExpRemainder}'");
        }
    }
}
