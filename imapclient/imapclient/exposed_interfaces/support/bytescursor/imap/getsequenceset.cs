using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        public bool GetSequenceSet(out cSequenceSet rSequenceSet)
        {
            var lBookmark = Position;

            List<cSequenceSet.cItem> lItems = new List<cSequenceSet.cItem>();

            while (true)
            {
                if (!ZGetSequenceSetItem(out var lItem))
                {
                    Position = lBookmark;
                    rSequenceSet = null;
                    return false;
                }

                lItems.Add(lItem);

                if (!SkipByte(cASCII.COMMA))
                {
                    rSequenceSet = new cSequenceSet(lItems);
                    return true;
                }
            }
        }

        private bool ZGetSequenceSetItem(out cSequenceSet.cItem rItem)
        {
            uint lNumber;
            cSequenceSet.cItem.cRangePart lItem;

            if (SkipByte(cASCII.ASTERISK)) lItem = cSequenceSet.cItem.Asterisk;
            else
            {
                if (!GetNZNumber(out _, out lNumber)) { rItem = null; return false; }
                lItem = new cSequenceSet.cItem.cNumber(lNumber);
            }

            var lBookmark = Position;

            if (!SkipByte(cASCII.COLON))
            {
                rItem = lItem;
                return true;
            }

            if (SkipByte(cASCII.ASTERISK))
            {
                rItem = new cSequenceSet.cItem.cRange(lItem, cSequenceSet.cItem.Asterisk);
                return true;
            }

            if (GetNZNumber(out _, out lNumber)) rItem = new cSequenceSet.cItem.cRange(lItem, new cSequenceSet.cItem.cNumber(lNumber));
            else
            {
                Position = lBookmark;
                rItem = lItem;
            }

            return true;
        }

        [Conditional("DEBUG")]
        private static void _Tests_SequenceSet(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(_Tests_SequenceSet));

            LTest("*", "cSequenceSet(cAsterisk())", null, new cUIntList(new uint[] { 15 }), "cSequenceSet(cNumber(15))");
            LTest("0", null, "0", null, null);
            LTest("2,4:7,9,12:*", "cSequenceSet(cNumber(2),cRange(cNumber(4),cNumber(7)),cNumber(9),cRange(cNumber(12),cAsterisk()))", null, new cUIntList(new uint[] { 2, 4, 5, 6, 7, 9, 12, 13, 14, 15 }), "cSequenceSet(cNumber(2),cRange(cNumber(4),cNumber(7)),cNumber(9),cRange(cNumber(12),cNumber(15)))");
            LTest("*:4,7:5", "cSequenceSet(cRange(cNumber(4),cAsterisk()),cRange(cNumber(5),cNumber(7)))", null, new cUIntList(new uint[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }), "cSequenceSet(cRange(cNumber(4),cNumber(15)))");

            void LTest(string pCursor, string pExpSeqSet, string pExpRemainder, cUIntList pExpList, string pExpSeqSet2)
            {
                TryConstruct(pCursor, out var lCursor);

                if (lCursor.GetSequenceSet(out var lSequenceSet))
                {
                    string lSeqSet = lSequenceSet.ToString();
                    if (lSeqSet != pExpSeqSet) throw new cTestsException($"failed to get expected sequence set from {pCursor}: '{lSeqSet}' vs '{pExpSeqSet}'");

                    var lList = cUIntList.FromSequenceSet(lSequenceSet, 15).ToSortedUniqueList();
                    if (pExpList.Count != lList.Count) throw new cTestsException($"failed to get expected uintlist from {lSequenceSet}");
                    for (int i = 0; i < pExpList.Count; i++) if (pExpList[i] != lList[i]) throw new cTestsException($"failed to get expected uintlist from {lSequenceSet}");

                    string lSeqSet2 = lList.ToSequenceSet().ToString();
                    if (lSeqSet2 != pExpSeqSet2) throw new cTestsException($"failed to get expected sequence set from {lList}: '{lSeqSet2}' vs '{pExpSeqSet2}'");
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
}