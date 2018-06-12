using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    }

    internal abstract class cSequenceSetItem
    {
        public static readonly cSequenceSetRangePart Asterisk = new cAsterisk();

        private class cAsterisk : cSequenceSetRangePart
        {
            public cAsterisk() { }

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
            if (pTo <= pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));
            From = new cSequenceSetNumber(pFrom);
            To = new cSequenceSetNumber(pTo);
        }

        public override string ToString() => $"{nameof(cSequenceSetRange)}({From},{To})";
    }
}