using System;
using System.Collections.Generic;
using System.Linq;

namespace work.bacome.imapinternals
{
    public partial class cSequenceSet
    {
        private struct sMergeMetric : IComparable<sMergeMetric>
        {
            public readonly long AdditionalValues;
            public readonly bool IsRangeRange;
            public readonly int GuaranteedSaving;
            public readonly int PotentialSaving;

            public sMergeMetric(long pAdditionalValues, bool pIsRangeRange, int pGuaranteedSaving, int pPotentialSaving)
            {
                if (pAdditionalValues < 1 || pAdditionalValues > uint.MaxValue - 2) throw new ArgumentOutOfRangeException(nameof(pAdditionalValues));
                if (pGuaranteedSaving < 0) throw new ArgumentOutOfRangeException(nameof(pGuaranteedSaving));
                if (pPotentialSaving < pGuaranteedSaving) throw new ArgumentOutOfRangeException(nameof(pPotentialSaving));

                AdditionalValues = pAdditionalValues;
                IsRangeRange = pIsRangeRange;
                GuaranteedSaving = pGuaranteedSaving;
                PotentialSaving = pPotentialSaving;
            }

            // for sorting measures by class
            public int CompareTo(sMergeMetric pOther)
            {
                int lCompareTo;
                if ((lCompareTo = AdditionalValues.CompareTo(pOther.AdditionalValues)) != 0) return lCompareTo; // adding less additional uints is better
                return pOther.IsRangeRange.CompareTo(IsRangeRange); // rangerange = true is better than rangerange = false
            }

            public override string ToString() => $"{nameof(sMergeMetric)}({AdditionalValues},{IsRangeRange},{GuaranteedSaving},{PotentialSaving})";
        }

        private struct sExtent
        {
            public readonly uint From;
            public readonly int FromASCIILength;
            public readonly uint To;
            public readonly int ToASCIILength;

            public sExtent(uint pFrom, uint pTo)
            {
                if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
                if (pTo < pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));

                From = pFrom;
                FromASCIILength = ZASCIILength(From);
                To = pTo;
                if (To == From) ToASCIILength = FromASCIILength;
                else ToASCIILength = ZASCIILength(To);
            }

            private sExtent(uint pFrom, int pFromASCIILength, uint pTo, int pToASCIILength)
            {
                if (pTo < pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));
                From = pFrom;
                FromASCIILength = pFromASCIILength;
                To = pTo;
                ToASCIILength = pToASCIILength;
            }

            public bool IsRange => To != From;
            public bool IsSingle => To == From;

            public sMergeMetric GetMergeMetric(sExtent pPrevious)
            {
                long lAdditionalValues = From - pPrevious.To - 1;
                int lGuaranteedSaving;

                if (pPrevious.IsRange)
                {
                    if (IsRange)
                    {
                        // range followed by range
                        lGuaranteedSaving = FromASCIILength + pPrevious.ToASCIILength + 2; // one comma and one of the colons
                        return new sMergeMetric(lAdditionalValues, true, lGuaranteedSaving, lGuaranteedSaving);
                    }

                    // range followed by single
                    lGuaranteedSaving = pPrevious.ToASCIILength + 1; // the comma
                    return new sMergeMetric(lAdditionalValues, false, lGuaranteedSaving, lGuaranteedSaving + FromASCIILength);
                }

                if (IsRange)
                {
                    // single followed by range
                    lGuaranteedSaving = FromASCIILength + 1; // the comma
                    return new sMergeMetric(lAdditionalValues, false, lGuaranteedSaving, lGuaranteedSaving + pPrevious.ToASCIILength); 
                }

                // single followed by single
                return new sMergeMetric(lAdditionalValues, false, 0, FromASCIILength);
            }

            public sMergeMetric GetMergeMetric(sExtent pPreviousSingle, sExtent pSingleBeforeThat)
            {
                if (!IsSingle) throw new InvalidOperationException();
                if (!pPreviousSingle.IsSingle) throw new ArgumentOutOfRangeException(nameof(pPreviousSingle));
                if (!pSingleBeforeThat.IsSingle) throw new ArgumentOutOfRangeException(nameof(pSingleBeforeThat));

                int lGuaranteedSaving = pPreviousSingle.FromASCIILength + 1; // 2 commas saved, but one colon added

                return new sMergeMetric(From - pSingleBeforeThat.To - 2, false, lGuaranteedSaving, lGuaranteedSaving + FromASCIILength); 
            }

            public sExtent GetMergedExtent(sExtent pPrevious) => new sExtent(pPrevious.From, pPrevious.FromASCIILength, To, ToASCIILength);

            public int ASCIILength
            {
                get
                {
                    if (IsSingle) return FromASCIILength;
                    return FromASCIILength + 1 + ToASCIILength; // + 1 is for the ':' between the numbers
                }
            }

            public cSequenceSetItem ToSequenceSetItem()
            {
                if (IsSingle) return new cSequenceSetNumber(From);
                return new cSequenceSetRange(From, To);
            }

            public override string ToString() => $"{nameof(sExtent)}({From},{FromASCIILength},{To},{ToASCIILength})";
        }

        private static List<sExtent> ZGetExtents(IEnumerable<uint> pUInts)
        {
            var lExtents = new List<sExtent>();

            bool lFirst = true;
            uint lFrom = 0;
            uint lTo = 0;

            foreach (var lUInt in pUInts.Distinct().OrderBy(i => i))
            {
                if (lFirst)
                {
                    lFirst = false;
                    lFrom = lUInt;
                    lTo = lUInt;
                }
                else if (lUInt == lTo + 1) lTo = lUInt;
                else
                {
                    lExtents.Add(new sExtent(lFrom, lTo));

                    lFrom = lUInt;
                    lTo = lUInt;
                }
            }

            if (lFirst) throw new ArgumentOutOfRangeException(nameof(pUInts));

            lExtents.Add(new sExtent(lFrom, lTo));

            return lExtents;
        }

        public static cSequenceSet FromUInts(int pASCIILengthLimit, IEnumerable<uint> pUInts)
        {
            const int kMinExtents = 4;

            if (pUInts == null) throw new ArgumentNullException(nameof(pUInts));

            var lExtents = ZGetExtents(pUInts);

            if (lExtents.Count < kMinExtents) return new cSequenceSet(new List<cSequenceSetItem>(from lExtent in lExtents select lExtent.ToSequenceSetItem()));

            int lASCIILength = lExtents.Count - 1; // commas
            foreach (var lExtent in lExtents) lASCIILength += lExtent.ASCIILength;

            if (lASCIILength <= pASCIILengthLimit) return new cSequenceSet(new List<cSequenceSetItem>(from lExtent in lExtents select lExtent.ToSequenceSetItem()));

            int lSavingsRequired = lASCIILength - pASCIILengthLimit;

            // have to merge extents

            // consider the following INITIAL merges ..
            //         range1 followed by range2                       : can be merged to range1.from->range2.to; this includes an additional        range2.from - range1.to - 1 VALUES;       and yields a saving of length(range1.to)+length(range2.from)+1(the comma)+1(one of the :)
            //    *  ! range followed by single                        : can be merged to range.from->single    ; this includes an additional        single - range.to - 1 VALUES;             and yields a saving of length(range.to)+1(the comma)
            //    *  ! single followed by a range                      : can be merged to single->range.to      ; this includes an additional        range.from - single - 1 VALUES;           and yields a saving of length(range.from)+1(the comma)
            //  x    ! single1 followed by single2                     : can be merged to single1->single2      ; this includes an additional        single2 - single1 - 1 VALUES;             and yields no savings at all
            //    *  ! single1 followed by single2 followed by single3 : can be merged to single1->single3      ; this includes an additional        single3 - single1 - 2 VALUES;             and yields a saving of length(single2)+1(2 commas saved, one : added)
            //
            //  once the merge is done, the merged range should be considered as replacing the things merged, meaning that it may then be possible to then do another merge [this is a CASCADING merge]
            //  in cases marked with a * the benefits of the CASCADING merges are not visible by analysing the set of INITIAL merges
            //   [ this explains why the case marked x is shown in the table above
            //     consider the case    range (3 values) single1 (3 values) single2 (3 values) single3 (3 values) 
            //     after merging range with single1, merging the resulting range with single2 makes sense (and so on), but before the initial merge, 
            //      no merge between the singles would be considered as adding less than 6 values when in fact merging them only adds 3 values each once the initial merge is done ]

            // the objective is to choose merges that minimise the number of additional VALUES included whilst getting to under the length limit 
            // a further objective is to minimise the time that the merging takes and this is where some complexity comes from

            // each INITIAL merge has a CLASS - the class is the combination of the number of VALUES added, and whether it could be involved in a cascde or not (merges marked above with a ! could be involved in a cascade)

            // CLASSES that add less VALUES are considered better, and within this range-range merges are considered better than other merges

            // each INITIAL merge has a POTENTIAL length saving (this is the saving that _might_ exist if the result of the merge is involved in a CASCADE)
            //  consider the following INITIAL merges ..
            //   range1 followed by range2                       : potential saving is actual saving
            //   range followed by single                        : potential saving of length(range.to)+1(the comma)+length(single) 
            //   single followed by a range                      : potential saving of length(range.from)+1(the comma)+length(single)
            //   single1 followed by single2                     : potential saving of length(single2) [single2 could be longer than single1]
            //   single1 followed by single2 followed by single3 : potential saving of length(single2)+1(2 commas saved, one : added)+length(single3) [ditto]

            // merging is done in a number of passes
            //  in each pass
            //   the CLASSes of merges to do are chosen
            //   then merges of those classes are done, stopping merging if the length limit is achieved
            //   if after doing all the chosen merges the length limit wasn't achieved, another pass is done

            // choosing the CLASSes of merge to do in each pass is the trick, choose to do too many, and more VALUEs than required will be added to acheive the length required

            while (true)
            {
                // analyse
                //////////

                // generate metrics

                var lMergeMetrics = new List<sMergeMetric>();

                for (int i = 1; i < lExtents.Count; i++)
                {
                    var lExtent = lExtents[i];
                    var lPreviousExtent = lExtents[i - 1];

                    lMergeMetrics.Add(lExtent.GetMergeMetric(lPreviousExtent));

                    if (lExtent.IsSingle && lPreviousExtent.IsSingle && i > 1)
                    {
                        var lPreviousPreviousExtent = lExtents[i - 2];
                        if (lPreviousPreviousExtent.IsSingle) lMergeMetrics.Add(lExtent.GetMergeMetric(lPreviousExtent, lPreviousPreviousExtent));
                    }
                }

                // sort metrics

                lMergeMetrics.Sort();

                // choose class

                sMergeMetric lDoMerge = lMergeMetrics[0];
                int lGuaranteedSavings = 0;
                int lPotentialSavings = 0;

                foreach (var lMergeMetric in lMergeMetrics)
                {
                    if (lMergeMetric.CompareTo(lDoMerge) == 1)
                    {
                        if (lGuaranteedSavings > 0 && lPotentialSavings > lSavingsRequired) break;
                        lDoMerge = lMergeMetric;
                    }

                    lGuaranteedSavings += lMergeMetric.GuaranteedSaving;
                    lPotentialSavings += lMergeMetric.PotentialSaving;
                }

                // merge
                ////////

                // merge in reverse order (because the number lengths are higher for the later entries)

                var lToMergeExtents = new Stack<sExtent>(lExtents);
                var lProcessedExtents = new Stack<sExtent>(lExtents.Count);

                while (lToMergeExtents.Count > 1)
                {
                    var lExtent = lToMergeExtents.Pop();
                    var lPreviousExtent = lToMergeExtents.Pop();

                    var lMergeMetric = lExtent.GetMergeMetric(lPreviousExtent);

                    if (lMergeMetric.GuaranteedSaving > 0 && lMergeMetric.CompareTo(lDoMerge) <= 0)
                    {
                        lToMergeExtents.Push(lExtent.GetMergedExtent(lPreviousExtent));
                        lSavingsRequired -= lMergeMetric.GuaranteedSaving;
                        if (lSavingsRequired < 1) break;
                        if (lExtent.IsSingle && lProcessedExtents.Count > 0) lToMergeExtents.Push(lProcessedExtents.Pop());
                        continue;
                    }

                    if (lExtent.IsSingle && lPreviousExtent.IsSingle && lToMergeExtents.Count > 1 && lToMergeExtents.Peek().IsSingle)
                    {
                        var lPreviousPreviousExtent = lToMergeExtents.Pop();

                        lMergeMetric = lExtent.GetMergeMetric(lPreviousExtent, lPreviousPreviousExtent);

                        if (lMergeMetric.GuaranteedSaving > 0 && lMergeMetric.CompareTo(lDoMerge) <= 0)
                        {
                            lToMergeExtents.Push(lExtent.GetMergedExtent(lPreviousPreviousExtent));
                            lSavingsRequired -= lMergeMetric.GuaranteedSaving;
                            if (lSavingsRequired < 1) break;
                            if (lProcessedExtents.Count > 0) lToMergeExtents.Push(lProcessedExtents.Pop());
                            continue;
                        }

                        lToMergeExtents.Push(lPreviousPreviousExtent);
                    }

                    lToMergeExtents.Push(lPreviousExtent);
                    lProcessedExtents.Push(lExtent);
                }

                lExtents.Clear();
                lExtents.AddRange(lToMergeExtents);
                lExtents.Reverse();
                lExtents.AddRange(lProcessedExtents);

                // check if we are done
                ///////////////////////

                if (lExtents.Count < kMinExtents || lSavingsRequired < 1) return new cSequenceSet(new List<cSequenceSetItem>(from lExtent in lExtents select lExtent.ToSequenceSetItem()));
            }
        }
    }
}