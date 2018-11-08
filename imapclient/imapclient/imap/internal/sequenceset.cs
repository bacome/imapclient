using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    internal class cSequenceSets : List<cSequenceSet>
    {
        public cSequenceSets() { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cSequenceSets));
            foreach (var lItem in this) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }

    internal class cSequenceSet : ReadOnlyCollection<cSequenceSetItem>
    {
        // note the commenting out of 1) and 2) below: these were causing problems when passing ranges to the server, as * means 'the last message', not infinity
        //  so there was no way to generate a range that was above everything which caused inconsistent behaviour in the > and >= operators for MSN filters 
        //  (and probably UID filters)

        public cSequenceSet(IList<cSequenceSetItem> pItems) : base(pItems) { }

        public cSequenceSet(uint pNumber) : base(ZFromNumber(pNumber)) { }

        public cSequenceSet(uint pFrom, uint pTo) : base(ZFromRange(pFrom, pTo)) { } 

        public bool Includes(uint pNumber, uint pAsterisk)
        {
            if (pNumber == 0) throw new ArgumentOutOfRangeException(nameof(pNumber));
            foreach (var lItem in this) if (lItem.CompareTo(pNumber, pAsterisk) == 0) return true;
            return false;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cSequenceSet));
            foreach (var lItem in this) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }

        private static cSequenceSetItem[] ZFromNumber(uint pNumber)
        {
            if (pNumber == 0) throw new ArgumentOutOfRangeException(nameof(pNumber));

            cSequenceSetItem[] lItems = new cSequenceSetItem[1];

            // 1) 
            //if (pNumber == uint.MaxValue) lItems[0] = cSequenceSetItem.Asterisk;
            //else

            lItems[0] = new cSequenceSetNumber(pNumber);

            return lItems;
        }

        private static cSequenceSetItem[] ZFromRange(uint pFrom, uint pTo)
        {
            if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
            if (pFrom > pTo) throw new ArgumentOutOfRangeException(nameof(pTo));

            cSequenceSetItem[] lItems = new cSequenceSetItem[1];

            // 2)
            //if (pTo == uint.MaxValue) lItems[0] = new cSequenceSetRange(new cSequenceSetNumber(pFrom), cSequenceSetItem.Asterisk);
            //else

            lItems[0] = new cSequenceSetRange(new cSequenceSetNumber(pFrom), new cSequenceSetNumber(pTo));

            return lItems;
        }

        public static cSequenceSet FromUInts(IEnumerable<uint> pUInts)
        {
            List<cSequenceSetItem> lItems = new List<cSequenceSetItem>();

            bool lFirst = true;
            uint lFrom = 0;
            uint lTo = 0;

            foreach (var lUInt in pUInts.Distinct().OrderBy(i => i))
            {
                if (lFirst)
                {
                    if (lUInt == 0) throw new ArgumentOutOfRangeException(nameof(pUInts));
                    lFrom = lUInt;
                    lTo = lUInt;
                    lFirst = false;
                }
                else if (lUInt == lTo + 1) lTo = lUInt;
                else
                {
                    LAddItem();
                    lFrom = lUInt;
                    lTo = lUInt;
                }
            }

            if (lFirst) throw new ArgumentOutOfRangeException(nameof(pUInts));

            LAddItem();

            return new cSequenceSet(lItems);

            void LAddItem()
            {
                if (lFrom == lTo) lItems.Add(new cSequenceSetNumber(lFrom));
                else lItems.Add(new cSequenceSetRange(lFrom, lTo));
            }
        }

        public static cSequenceSet FromUInts(IEnumerable<uint> pUInts, int pMaxNumberCount)
        {
            List<cSequenceSetItem> lItems = new List<cSequenceSetItem>();

            foreach (var lRange in ZGetRangesCoveringUInts(pUInts, pMaxNumberCount))
            {
                if (lRange.IsSingle) lItems.Add(new cSequenceSetNumber(lRange.From));
                else lItems.Add(new cSequenceSetRange(lRange.From, lRange.To));
            }

            return new cSequenceSet(lItems);
        }

        private static List<sUIntRange> ZGetRangesCoveringUInts(IEnumerable<uint> pUInts, int pMaxNumberCount)
        {
            if (pUInts == null) throw new ArgumentNullException(nameof(pUInts));
            if (pMaxNumberCount < 4) throw new ArgumentOutOfRangeException(nameof(pMaxNumberCount));

            // build an initial list of ranges

            var lRanges = new List<sUIntRange>();

            int lNumberCount = 0;

            bool lFirst = true;
            uint lFrom = 0;
            uint lTo = 0;

            foreach (var lUInt in pUInts.Distinct().OrderBy(i => i))
            {
                if (lFirst)
                {
                    if (lUInt == 0) throw new ArgumentOutOfRangeException(nameof(pUInts));
                    lFrom = lUInt;
                    lTo = lUInt;
                    lFirst = false;
                }
                else if (lUInt == lTo + 1) lTo = lUInt;
                else
                {
                    lRanges.Add(new sUIntRange(lFrom, lTo));
                    if (lTo == lFrom) lNumberCount++;
                    else lNumberCount += 2;

                    lFrom = lUInt;
                    lTo = lUInt;
                }
            }

            if (lFirst) throw new ArgumentOutOfRangeException(nameof(pUInts));

            lRanges.Add(new sUIntRange(lFrom, lTo));
            if (lTo == lFrom) lNumberCount++;
            else lNumberCount += 2;

            // built

            while (lNumberCount > pMaxNumberCount)
            {
                // have to coalesce ranges

                uint lSmallestGapSize = uint.MaxValue;
                int lSmallestGapLargestSaving = 0;

                var lGapSizeToCount = new SortedDictionary<uint, int>();

                uint lGapSize;
                uint lLastTo;
                sUIntRange lPendingSingle1;
                sUIntRange lPendingSingle2;

                // analyse pass
                //  find the smallest closable gap between the ranges and the largest number of numbers we can save by closing gaps of that size
                //  get a list of gap sizes with counts

                lLastTo = 0;
                lPendingSingle1 = sUIntRange.Zero;
                lPendingSingle2 = sUIntRange.Zero;

                foreach (var lRange in lRanges)
                {
                    if (lLastTo == 0) lGapSize = 0;
                    else
                    {
                        lGapSize = lRange.From - lLastTo - 1;

                        int lCount;
                        if (lGapSizeToCount.TryGetValue(lGapSize, out lCount)) lCount++;
                        else lCount = 1;

                        lGapSizeToCount[lGapSize] = lCount;
                    }

                    lLastTo = lRange.To;

                    if (lRange.IsSingle)
                    {
                        if (lPendingSingle1.IsZero)
                        {
                            if (lGapSize != 0 && lGapSize < lSmallestGapSize)
                            {
                                lSmallestGapSize = lGapSize;
                                lSmallestGapLargestSaving = 1;
                            }

                            lPendingSingle1 = lRange;
                        }
                        else if (lPendingSingle2.IsZero) lPendingSingle2 = lRange;
                        else
                        {
                            var lSinglesGapSize = lPendingSingle2.From - lPendingSingle1.To - 1 + lGapSize;

                            if (lSinglesGapSize < lSmallestGapSize)
                            {
                                lSmallestGapSize = lSinglesGapSize;
                                lSmallestGapLargestSaving = 1;
                            }

                            lPendingSingle1 = lPendingSingle2;
                            lPendingSingle2 = lRange;
                        }
                    }
                    else
                    {
                        if (lGapSize != 0)
                        {
                            if (lPendingSingle1.IsZero && lGapSize <= lSmallestGapSize)
                            {
                                lSmallestGapSize = lGapSize;
                                lSmallestGapLargestSaving = 2;
                            }
                            else if (lGapSize < lSmallestGapSize)
                            {
                                lSmallestGapSize = lGapSize;
                                lSmallestGapLargestSaving = 1;
                            }
                        }

                        lPendingSingle1 = sUIntRange.Zero;
                        lPendingSingle2 = sUIntRange.Zero;
                    }
                }

                // choose the limit for the coalesce pass

                uint lLimitGapSize;
                int lLimitGapSizeRequiredSaving;

                var lNumbersRequiredToSave = lNumberCount - pMaxNumberCount;

                lGapSize = 0;

                foreach (var lPair in lGapSizeToCount)
                {
                    lNumbersRequiredToSave -= lPair.Value * 2; // the maximum saving from closing a gap is two numbers
                    if (lNumbersRequiredToSave < 0) break;
                    lGapSize = lPair.Key;
                }

                if (lGapSize > lSmallestGapSize)
                {
                    lLimitGapSize = lGapSize;
                    lLimitGapSizeRequiredSaving = 2;
                }
                else
                {
                    lLimitGapSize = lSmallestGapSize;
                    lLimitGapSizeRequiredSaving = lSmallestGapLargestSaving;
                }

                // coalesce pass

                var lNewRanges = new List<sUIntRange>(lRanges.Count);

                lLastTo = 0;
                lPendingSingle1 = sUIntRange.Zero;
                lPendingSingle2 = sUIntRange.Zero;
                sUIntRange lPendingRange = sUIntRange.Zero;

                foreach (var lRange in lRanges)
                {
                    if (lLastTo == 0)
                    {
                        if (lRange.IsSingle) lPendingSingle1 = lRange;
                        else lPendingRange = lRange;
                    }
                    else
                    {
                        lGapSize = lRange.From - lLastTo - 1;

                        var lCurrentRange = lRange;

                        while (true)
                        {

                            if (lCurrentRange.IsSingle)
                            {
                                if (lPendingSingle1.IsZero)
                                {
                                    if (lGapSize < lLimitGapSize || (lGapSize == lLimitGapSize && lLimitGapSizeRequiredSaving == 1))
                                    {
                                        lCurrentRange = new sUIntRange(lPendingRange.From, lCurrentRange.To);
                                        if (--lNumberCount == pMaxNumberCount) lLimitGapSize = 0;
                                        lPendingRange = sUIntRange.Zero;
                                    }
                                    else
                                    {
                                        lNewRanges.Add(lPendingRange);
                                        lPendingRange = sUIntRange.Zero;
                                        lPendingSingle1 = lCurrentRange;
                                        break;
                                    }
                                }
                                else if (lPendingSingle2.IsZero)
                                {
                                    lPendingSingle2 = lCurrentRange;
                                    break;
                                }
                                else
                                {
                                    var lSinglesGapSize = lPendingSingle2.From - lPendingSingle1.To - 1 + lGapSize;

                                    if (lSinglesGapSize < lLimitGapSize || (lSinglesGapSize == lLimitGapSize && lLimitGapSizeRequiredSaving == 1))
                                    {
                                        lCurrentRange = new sUIntRange(lPendingSingle1.From, lCurrentRange.To);
                                        if (--lNumberCount == pMaxNumberCount) lLimitGapSize = 0;
                                        lPendingSingle1 = sUIntRange.Zero;
                                        lPendingSingle2 = sUIntRange.Zero;
                                    }
                                    else
                                    {
                                        lNewRanges.Add(lPendingSingle1);
                                        lPendingSingle1 = lPendingSingle2;
                                        lPendingSingle2 = lCurrentRange;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (lGapSize < lLimitGapSize || (lGapSize == lLimitGapSize && (lPendingSingle1.IsZero || lLimitGapSizeRequiredSaving == 1)))
                                {
                                    if (lPendingSingle1.IsZero)
                                    {
                                        lCurrentRange = new sUIntRange(lPendingRange.From, lCurrentRange.To);
                                        lNumberCount -= 2;
                                        if (lNumberCount <= pMaxNumberCount) lLimitGapSize = 0;
                                        lPendingRange = sUIntRange.Zero;
                                    }
                                    else
                                    {
                                        if (!lPendingSingle2.IsZero)
                                        {
                                            lNewRanges.Add(lPendingSingle1);
                                            lPendingSingle1 = lPendingSingle2;
                                            lPendingSingle2 = sUIntRange.Zero;
                                        }

                                        lCurrentRange = new sUIntRange(lPendingSingle1.From, lCurrentRange.To);
                                        if (--lNumberCount == pMaxNumberCount) lLimitGapSize = 0;
                                        lPendingSingle1 = sUIntRange.Zero;
                                    }
                                }
                                else
                                {
                                    if (!lPendingSingle1.IsZero) lNewRanges.Add(lPendingSingle1);
                                    if (!lPendingSingle2.IsZero) lNewRanges.Add(lPendingSingle2);
                                    if (!lPendingRange.IsZero) lNewRanges.Add(lPendingRange);
                                    lPendingSingle1 = sUIntRange.Zero;
                                    lPendingSingle2 = sUIntRange.Zero;
                                    lPendingRange = lCurrentRange;
                                    break;
                                }
                            }

                            if (lNewRanges.Count == 0 || lLimitGapSize == 0)
                            {
                                lPendingRange = lCurrentRange;
                                break;
                            }

                            // "pop" off the required ranges into the right places so we can process the newly created range

                            var lPoppedRange = lNewRanges[lNewRanges.Count - 1];
                            lNewRanges.RemoveAt(lNewRanges.Count - 1);

                            lGapSize = lCurrentRange.From - lPoppedRange.To - 1;

                            if (lPoppedRange.IsSingle)
                            {
                                lPendingSingle1 = lPoppedRange;

                                if (lNewRanges.Count != 0)
                                {
                                    var lPeekedRange = lNewRanges[lNewRanges.Count - 1];

                                    if (lPeekedRange.IsSingle)
                                    {
                                        lPendingSingle2 = lPendingSingle1;
                                        lPendingSingle1 = lPeekedRange;
                                        lNewRanges.RemoveAt(lNewRanges.Count - 1);
                                    }
                                }
                            }
                            else lPendingRange = lPoppedRange;
                        }
                    }

                    lLastTo = lRange.To;
                }

                if (!lPendingSingle1.IsZero) lNewRanges.Add(lPendingSingle1);
                if (!lPendingSingle2.IsZero) lNewRanges.Add(lPendingSingle2);
                if (!lPendingRange.IsZero) lNewRanges.Add(lPendingRange);

                lRanges = lNewRanges;
            }

            return lRanges;
        }

        private struct sUIntRange
        {
            public static readonly sUIntRange Zero = new sUIntRange();

            public readonly uint From;
            public readonly uint To;

            public sUIntRange(uint pFrom, uint pTo)
            {
                if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
                if (pTo < pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));

                From = pFrom;
                To = pTo;
            }

            public bool IsSingle => To == From;
            public bool IsZero => From == 0;
        }

        [Conditional("DEBUG")]
        internal static void _Tests()
        {
            // parsing and construction tests
            _Tests_1("*", "cSequenceSet(cAsterisk())", null, new cUIntList(new uint[] { 15 }), "cSequenceSet(cSequenceSetNumber(15))");
            _Tests_1("0", null, "0", null, null);
            _Tests_1("2,4:7,9,12:*", "cSequenceSet(cSequenceSetNumber(2),cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(7)),cSequenceSetNumber(9),cSequenceSetRange(cSequenceSetNumber(12),cAsterisk()))", null, new cUIntList(new uint[] { 2, 4, 5, 6, 7, 9, 12, 13, 14, 15 }), "cSequenceSet(cSequenceSetNumber(2),cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(7)),cSequenceSetNumber(9),cSequenceSetRange(cSequenceSetNumber(12),cSequenceSetNumber(15)))");
            _Tests_1("*:4,7:5", "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(4),cAsterisk()),cSequenceSetRange(cSequenceSetNumber(5),cSequenceSetNumber(7)))", null, new cUIntList(new uint[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }), "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(4),cSequenceSetNumber(15)))");

            // coalesce and include tests
            _Tests_2(new uint[] { 1, 2, 3, 4, 5 }, 5, "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(1),cSequenceSetNumber(5)))", new uint[] { 1, 2, 3, 4, 5 }, new uint[] { 0, 6, 7 });
            _Tests_2(new uint[] { 1, 2, 3, 4, 5, 6 }, 5, "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(1),cSequenceSetNumber(6)))", new uint[] { 1, 2, 3, 4, 5, 6 }, new uint[] { 0, 7, 8 });
            _Tests_2(new uint[] { 1, 2, 3, 4, 5, 7, 8, 9, 14 }, 5, "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(1),cSequenceSetNumber(5)),cSequenceSetRange(cSequenceSetNumber(7),cSequenceSetNumber(9)),cSequenceSetNumber(14))", new uint[] { 1, 2, 3, 4, 5, 7, 8, 9, 14 }, new uint[] { 0, 6, 10, 11, 12, 13, 15, 16 });
            _Tests_2(new uint[] { 1, 2, 3, 4, 5, 7, 8, 9, 14, 15 }, 5, "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(1),cSequenceSetNumber(9)),cSequenceSetRange(cSequenceSetNumber(14),cSequenceSetNumber(15)))", new uint[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 15 }, new uint[] { 0, 10, 11, 12, 13, 16, 17 });
            _Tests_2(new uint[] { 1, 2, 3, 4, 5, 12, 13, 14, 16, 17 }, 5, "cSequenceSet(cSequenceSetRange(cSequenceSetNumber(1),cSequenceSetNumber(5)),cSequenceSetRange(cSequenceSetNumber(12),cSequenceSetNumber(17)))", new uint[] { 1, 2, 3, 4, 5, 12, 13, 14, 15, 16, 17 }, new uint[] { 0, 6, 7, 8, 9, 10, 11, 18, 19 });

            // includes tests with *
            cSequenceSet lAsterisk = new cSequenceSet(new cSequenceSetItem[] { cSequenceSetItem.Asterisk });
            _Tests_3(lAsterisk, 5, new uint[] { 5 }, new uint[] { 3, 4, 6, 7 });

            cSequenceSet lTwoRanges = new cSequenceSet(new cSequenceSetItem[] { new cSequenceSetRange(5, 10), new cSequenceSetRange(new cSequenceSetNumber(25), cSequenceSetItem.Asterisk) });
            _Tests_3(lTwoRanges, 30, new uint[] { 5, 6, 7, 8, 9, 10, 25, 26, 27, 28, 29, 30 }, new uint[] { 3, 4, 11, 12, 23, 24, 31, 32 });
            _Tests_3(lTwoRanges, 20, new uint[] { 5, 6, 7, 8, 9, 10, 20, 21, 22, 23, 24, 25 }, new uint[] { 3, 4, 11, 12, 18, 19, 26, 27 });
        }

        private static void _Tests_1(string pCursor, string pExpSeqSet, string pExpRemainder, cUIntList pExpList, string pExpSeqSet2)
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

        private static void _Tests_2(IEnumerable<uint> pUInts, int pMaxNumberCount, string pExpSeqSet, IEnumerable<uint> pIncludes, IEnumerable<uint> pExcludes)
        {
            var lSeqSet = FromUInts(pUInts, pMaxNumberCount);
            if (lSeqSet.ToString() != pExpSeqSet) throw new cTestsException($"failed to get expected sequence set from {new cUIntList(pUInts)}: got '{lSeqSet}' vs expected '{pExpSeqSet}'");
            _Tests_3(lSeqSet, 100, pIncludes, pExcludes);
        }

        private static void _Tests_3(cSequenceSet pSeqSet, uint pAsterisk, IEnumerable<uint> pIncludes, IEnumerable<uint> pExcludes)
        {
            foreach (var lUInt in pIncludes) if (!pSeqSet.Includes(lUInt, pAsterisk)) throw new cTestsException($"sequence set {pSeqSet} doesn't include {lUInt}");
            foreach (var lUInt in pExcludes) if (pSeqSet.Includes(lUInt, pAsterisk)) throw new cTestsException($"sequence set {pSeqSet} doesn't exclude {lUInt}");
        }
    }

    internal abstract class cSequenceSetItem
    {
        public static readonly cSequenceSetRangePart Asterisk = new cAsterisk();

        public abstract int CompareTo(uint pNumber, uint pAsterisk);

        private class cAsterisk : cSequenceSetRangePart
        {
            public cAsterisk() { }

            public override int CompareTo(uint pNumber, uint pAsterisk)
            {
                if (pAsterisk == 0) throw new ArgumentOutOfRangeException(nameof(pAsterisk));
                return pAsterisk.CompareTo(pNumber);
            }

            public override int CompareTo(cSequenceSetRangePart pOther)
            {
                if (pOther == null) throw new ArgumentOutOfRangeException(nameof(pOther));
                if (pOther == Asterisk) return 0;
                return 1;
            }

            public override string ToString() => $"{nameof(cAsterisk)}()";
        }
    }

    internal abstract class cSequenceSetRangePart : cSequenceSetItem
    {
        public abstract int CompareTo(cSequenceSetRangePart pOther);
    }

    internal class cSequenceSetNumber : cSequenceSetRangePart
    {
        public readonly uint Number;

        public cSequenceSetNumber(uint pNumber)
        {
            if (pNumber == 0) throw new ArgumentOutOfRangeException(nameof(pNumber));
            Number = pNumber;
        }

        public override int CompareTo(uint pNumber, uint pAsterisk) => Number.CompareTo(pNumber);

        public override int CompareTo(cSequenceSetRangePart pOther)
        {
            if (pOther == Asterisk) return -1;
            if (!(pOther is cSequenceSetNumber lOther)) throw new ArgumentOutOfRangeException(nameof(pOther));
            return Number.CompareTo(lOther.Number);
        }

        public override string ToString() => $"{nameof(cSequenceSetNumber)}({Number})";
    }

    internal class cSequenceSetRange : cSequenceSetItem
    {
        public readonly cSequenceSetRangePart From;
        public readonly cSequenceSetRangePart To;

        public cSequenceSetRange(cSequenceSetRangePart pLeft, cSequenceSetRangePart pRight)
        {
            if (pLeft == null) throw new ArgumentNullException(nameof(pLeft));
            if (pRight == null) throw new ArgumentNullException(nameof(pRight));
            if (pLeft.CompareTo(pRight) == 1) { From = pRight; To = pLeft; }
            else { From = pLeft; To = pRight; }
        }

        public cSequenceSetRange(uint pFrom, uint pTo)
        {
            if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
            if (pTo <= pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));
            From = new cSequenceSetNumber(pFrom);
            To = new cSequenceSetNumber(pTo);
        }

        public override int CompareTo(uint pNumber, uint pAsterisk)
        {
            uint lFrom;

            if (From is cSequenceSetNumber lSSNFrom) lFrom = lSSNFrom.Number;
            else
            {
                if (pAsterisk == 0) throw new ArgumentOutOfRangeException(nameof(pAsterisk));
                lFrom = pAsterisk;
            }

            uint lTo;

            if (To is cSequenceSetNumber lSSNTo) lTo = lSSNTo.Number;
            else
            {
                if (pAsterisk == 0) throw new ArgumentOutOfRangeException(nameof(pAsterisk));
                lTo = pAsterisk;
            }

            if (lFrom > lTo)
            {
                var lTemp = lFrom;
                lFrom = lTo;
                lTo = lTemp;
            }

            int lResult;

            lResult = lFrom.CompareTo(pNumber);

            if (lResult == 1)
            {
                if (lTo.CompareTo(pNumber) == 1) return 1;
                else return 0;
            }

            return lResult;
        }

        public override string ToString() => $"{nameof(cSequenceSetRange)}({From},{To})";
    }
}