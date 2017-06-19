using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    public class cSequenceSet : ReadOnlyCollection<cSequenceSet.cItem>
    {
        public cSequenceSet(IList<cItem> pItems) : base(pItems) { }

        public cSequenceSet(uint pNumber) : base(ZFromNumber(pNumber)) { }

        public cSequenceSet(uint pFrom, uint pTo) : base(ZFromRange(pFrom, pTo)) { } 

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cSequenceSet));
            foreach (var lItem in this) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }

        private static cItem[] ZFromNumber(uint pNumber)
        {
            if (pNumber == 0) throw new ArgumentOutOfRangeException(nameof(pNumber));

            cItem[] lItems = new cItem[1];

            if (pNumber == uint.MaxValue) lItems[0] = cItem.Asterisk;
            else lItems[0] = new cItem.cNumber(pNumber);

            return lItems;
        }

        private static cItem[] ZFromRange(uint pFrom, uint pTo)
        {
            if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
            if (pFrom > pTo) throw new ArgumentOutOfRangeException(nameof(pTo));

            cItem[] lItems = new cItem[1];

            if (pTo == uint.MaxValue) lItems[0] = new cItem.cRange(new cItem.cNumber(pFrom), cItem.Asterisk);
            else lItems[0] = new cItem.cRange(new cItem.cNumber(pFrom), new cItem.cNumber(pTo));

            return lItems;
        }

        public abstract class cItem
        {
            public static readonly cRangePart Asterisk = new cAsterisk();

            public abstract class cRangePart : cItem, IComparable<cRangePart>
            {
                public abstract int CompareTo(cRangePart pOther);
            }

            private class cAsterisk : cRangePart
            {
                public cAsterisk() { }

                public override int CompareTo(cRangePart pOther)
                {
                    if (pOther == null) throw new ArgumentOutOfRangeException(nameof(pOther));
                    if (pOther == Asterisk) return 0;
                    return 1;
                }

                public override string ToString() => $"{nameof(cAsterisk)}()";
            }

            public class cNumber : cRangePart
            {
                public readonly uint Number;

                public cNumber(uint pNumber)
                {
                    if (pNumber == 0) throw new ArgumentOutOfRangeException(nameof(pNumber));
                    Number = pNumber;
                }

                public override int CompareTo(cRangePart pOther)
                {
                    if (pOther == Asterisk) return -1;
                    if (!(pOther is cNumber lOther)) throw new ArgumentOutOfRangeException(nameof(pOther));
                    return Number.CompareTo(lOther.Number);
                }

                public override string ToString() => $"{nameof(cNumber)}({Number})";
            }

            public class cRange : cItem
            {
                public readonly cRangePart From;
                public readonly cRangePart To;

                public cRange(cRangePart pLeft, cRangePart pRight)
                {
                    if (pLeft == null) throw new ArgumentNullException(nameof(pLeft));
                    if (pRight == null) throw new ArgumentNullException(nameof(pRight));
                    if (pLeft.CompareTo(pRight) == 1) { From = pRight; To = pLeft; }
                    else { From = pLeft; To = pRight; }
                }

                public cRange(uint pFrom, uint pTo)
                {
                    if (pTo <= pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));
                    From = new cNumber(pFrom);
                    To = new cNumber(pTo);
                }

                public override string ToString() => $"{nameof(cRange)}({From},{To})";
            }
        }
    }
}