using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace work.bacome.imapclient.support
{
    public class cSequenceSet : ReadOnlyCollection<cSequenceSetItem>
    {
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

            if (pNumber == uint.MaxValue) lItems[0] = cSequenceSetItem.Asterisk;
            else lItems[0] = new cSequenceSetNumber(pNumber);

            return lItems;
        }

        private static cSequenceSetItem[] ZFromRange(uint pFrom, uint pTo)
        {
            if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
            if (pFrom > pTo) throw new ArgumentOutOfRangeException(nameof(pTo));

            cSequenceSetItem[] lItems = new cSequenceSetItem[1];

            if (pTo == uint.MaxValue) lItems[0] = new cSequenceSetRange(new cSequenceSetNumber(pFrom), cSequenceSetItem.Asterisk);
            else lItems[0] = new cSequenceSetRange(new cSequenceSetNumber(pFrom), new cSequenceSetNumber(pTo));

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
    }

    public abstract class cSequenceSetItem
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

    public abstract class cSequenceSetRangePart : cSequenceSetItem
    {
        public abstract int CompareTo(cSequenceSetRangePart pOther);
    }

    public class cSequenceSetNumber : cSequenceSetRangePart
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

    public class cSequenceSetRange : cSequenceSetItem
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