using System;
using System.Collections.Generic;
using System.Linq;

namespace work.bacome.imapclient
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

        public static cUIntList FromUInt(uint pUInt)
        {
            var lResult = new cUIntList();
            lResult.Add(pUInt);
            return lResult;
        }

        public static bool TryConstruct(cSequenceSet pSequenceSet, uint pAsterisk, bool pDistinct, out cUIntList rResult)
        {
            if (pSequenceSet == null) throw new ArgumentNullException(nameof(pSequenceSet));
            if (!ZTryExpand(pSequenceSet, pAsterisk, out rResult)) return false;
            if (pDistinct) rResult = new cUIntList(rResult.Distinct());
            return true;
        }

        public static bool TryConstruct(IEnumerable<cSequenceSet> pSequenceSets, uint pAsterisk, bool pDistinct, out cUIntList rResult)
        {
            if (pSequenceSets == null) throw new ArgumentNullException(nameof(pSequenceSets));

            rResult = new cUIntList();

            foreach (var lSequenceSet in pSequenceSets)
            {
                if (lSequenceSet == null) throw new ArgumentOutOfRangeException(nameof(pSequenceSets), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (!ZTryExpand(lSequenceSet, pAsterisk, out var lResult)) return false;
                rResult.AddRange(lResult);
            }

            if (pDistinct) rResult = new cUIntList(rResult.Distinct());
            return true;
        }

        private static bool ZTryExpand(cSequenceSet pSequenceSet, uint pAsterisk, out cUIntList rResult)
        {
            if (pSequenceSet == null) { rResult = null; return false; }

            rResult = new cUIntList();

            foreach (var lItem in pSequenceSet)
            {
                if (!lItem.TryExpand(pAsterisk, out var lResult)) return false;
                rResult.AddRange(lResult);
            }

            return true;
        }
    }
}