using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public class cUIntList : List<uint>
    {




        ;?; // linq this






        public cUIntList() : base() { }

        public cUIntList(IEnumerable<uint> pUInts) : base(pUInts) { }

        public cUIntList ToSortedUniqueList()
        {
            cUIntList lResult = new cUIntList();

            cUIntList lWork = new cUIntList(this);
            lWork.Sort();

            bool lFirst = true;
            uint lLast = 0;

            foreach (uint lUInt in lWork)
            {
                if (lFirst) lFirst = false;
                else if (lUInt == lLast) continue;
                lResult.Add(lUInt);
                lLast = lUInt;
            }

            return lResult;
        }

        public cSequenceSet ToSequenceSet()
        {
            List<cSequenceSetItem> lItems = new List<cSequenceSetItem>();

            cUIntList lWork = ToSortedUniqueList();

            bool lFirst = true;
            uint lFrom = 0;
            uint lTo = 0;

            foreach (uint lUInt in lWork)
            {
                if (lFirst)
                {
                    lFrom = lUInt;
                    lTo = lUInt;
                    lFirst = false;
                }
                else
                {
                    if (lUInt == lTo + 1) lTo = lUInt;
                    else
                    {
                        LAddItem();
                        lFrom = lUInt;
                        lTo = lUInt;
                    }
                }
            }

            if (!lFirst) LAddItem();

            return new cSequenceSet(lItems);

            void LAddItem()
            {
                if (lFrom == lTo) lItems.Add(new cSequenceSetNumber(lFrom));
                else lItems.Add(new cSequenceSetRange(lFrom, lTo));
            }
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIntList));
            foreach (var lUInt in this) lBuilder.Append(lUInt);
            return lBuilder.ToString();
        }

        public static cUIntList FromSequenceSet(cSequenceSet pSequenceSet, uint pAsterisk) => ZExpand(pSequenceSet, pAsterisk);

        public static cUIntList FromSequenceSets(cSequenceSets pSequenceSets, uint pAsterisk)
        {
            if (pSequenceSets == null) throw new ArgumentNullException(nameof(pSequenceSets));
            cUIntList lResult = new cUIntList();
            foreach (var lSequenceSet in pSequenceSets) lResult.AddRange(ZExpand(lSequenceSet, pAsterisk));
            return lResult;
        }

        private static cUIntList ZExpand(cSequenceSet pSequenceSet, uint pAsterisk)
        {
            if (pSequenceSet == null) throw new ArgumentNullException(nameof(pSequenceSet));

            cUIntList lResult = new cUIntList();

            foreach (var lItem in pSequenceSet)
            {
                if (lItem == cSequenceSetItem.Asterisk)
                {
                    lResult.Add(pAsterisk);
                    continue;
                }

                if (lItem is cSequenceSetNumber lNumber)
                {
                    lResult.Add(lNumber.Number);
                    continue;
                }

                if (!(lItem is cSequenceSetRange lRange)) throw new ArgumentException("invalid form 1", nameof(pSequenceSet));

                if (lRange.From == cSequenceSetItem.Asterisk)
                {
                    lResult.Add(pAsterisk);
                    continue;
                }

                if (!(lRange.From is cSequenceSetNumber lFrom)) throw new ArgumentException("invalid form 2", nameof(pSequenceSet));

                uint lTo;

                if (lRange.To == cSequenceSetItem.Asterisk) lTo = pAsterisk;
                else
                {
                    if (!(lRange.To is cSequenceSetNumber lRangeTo)) throw new ArgumentException("invalid form 3", nameof(pSequenceSet));
                    lTo = lRangeTo.Number;
                }

                for (uint i = lFrom.Number; i <= lTo; i++) lResult.Add(i);
            }

            return lResult;
        }
    }
}