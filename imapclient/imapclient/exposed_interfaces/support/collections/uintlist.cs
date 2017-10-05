using System;
using System.Collections.Generic;
using System.Linq;

namespace work.bacome.imapclient.support
{
    public class cUIntList : List<uint>
    {
        public cUIntList() : base() { }
        public cUIntList(IEnumerable<uint> pUInts) : base(pUInts) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIntList));
            foreach (var lUInt in this) lBuilder.Append(lUInt);
            return lBuilder.ToString();
        }

        public static cUIntList FromSequenceSet(cSequenceSet pSequenceSet, int pAsterisk, bool pDistinct)
        {
            if (pSequenceSet == null) throw new ArgumentNullException(nameof(pSequenceSet));
            if (pAsterisk < 0) throw new ArgumentOutOfRangeException(nameof(pAsterisk));

            var lResult = ZExpand(pSequenceSet, (uint)pAsterisk);

            if (pDistinct) lResult = new cUIntList(lResult.Distinct());
            return lResult;
        }

        public static cUIntList FromSequenceSets(cSequenceSets pSequenceSets, int pAsterisk, bool pDistinct)
        {
            if (pSequenceSets == null) throw new ArgumentNullException(nameof(pSequenceSets));
            if (pAsterisk < 0) throw new ArgumentOutOfRangeException(nameof(pAsterisk));

            cUIntList lResult = new cUIntList();
            uint lAsterisk = (uint)pAsterisk;
            foreach (var lSequenceSet in pSequenceSets) lResult.AddRange(ZExpand(lSequenceSet, lAsterisk));

            if (pDistinct) lResult = new cUIntList(lResult.Distinct());
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