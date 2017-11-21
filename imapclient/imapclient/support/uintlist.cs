using System;
using System.Collections.Generic;
using System.Linq;

namespace work.bacome.imapclient
{
    internal class cUIntList : List<uint>
    {
        public cUIntList() : base() { }
        public cUIntList(IEnumerable<uint> pUInts) : base(pUInts) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIntList));
            foreach (var lUInt in this) lBuilder.Append(lUInt);
            return lBuilder.ToString();
        }

        internal static bool TryConstruct(cSequenceSet pSequenceSet, int pAsterisk, bool pDistinct, out cUIntList rResult)
        {
            if (pSequenceSet == null) throw new ArgumentNullException(nameof(pSequenceSet));
            if (!ZExpand(pSequenceSet, pAsterisk, out rResult)) return false;
            if (pDistinct) rResult = new cUIntList(rResult.Distinct());
            return true;
        }

        internal static bool TryConstruct(cSequenceSets pSequenceSets, int pAsterisk, bool pDistinct, out cUIntList rResult)
        {
            if (pSequenceSets == null) throw new ArgumentNullException(nameof(pSequenceSets));

            rResult = new cUIntList();

            foreach (var lSequenceSet in pSequenceSets)
            {
                if (lSequenceSet == null) return false;
                if (!ZExpand(lSequenceSet, pAsterisk, out var lResult)) return false;
                rResult.AddRange(lResult);
            }

            if (pDistinct) rResult = new cUIntList(rResult.Distinct());
            return true;
        }

        private static bool ZExpand(cSequenceSet pSequenceSet, int pAsterisk, out cUIntList rResult)
        {
            if (pSequenceSet == null) { rResult = null; return false; }

            rResult = new cUIntList();

            foreach (var lItem in pSequenceSet)
            {
                if (lItem == cSequenceSetItem.Asterisk)
                {
                    if (pAsterisk < 1) return false;
                    rResult.Add((uint)pAsterisk);
                    continue;
                }

                if (lItem is cSequenceSetNumber lNumber)
                {
                    rResult.Add(lNumber.Number);
                    continue;
                }

                if (!(lItem is cSequenceSetRange lRange)) return false;

                if (lRange.From == cSequenceSetItem.Asterisk)
                {
                    if (pAsterisk < 1) return false;
                    rResult.Add((uint)pAsterisk);
                    continue;
                }

                if (!(lRange.From is cSequenceSetNumber lFrom)) return false;

                uint lTo;

                if (lRange.To == cSequenceSetItem.Asterisk)
                {
                    if (pAsterisk < 1) return false;
                    lTo = (uint)pAsterisk;
                }
                else
                {
                    if (!(lRange.To is cSequenceSetNumber lRangeTo)) return false;
                    lTo = lRangeTo.Number;
                }

                for (uint i = lFrom.Number; i <= lTo; i++) rResult.Add(i);
            }

            return true;
        }
    }
}