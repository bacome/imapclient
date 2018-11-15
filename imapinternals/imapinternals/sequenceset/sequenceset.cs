using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace work.bacome.imapinternals
{
    public partial class cSequenceSet : ReadOnlyCollection<cSequenceSetItem>
    {
        public cSequenceSet(IList<cSequenceSetItem> pItems) : base(pItems)
        {
            if (pItems.Count == 0) throw new ArgumentOutOfRangeException(nameof(pItems));
            foreach (var lItem in pItems) if (lItem == null) throw new ArgumentOutOfRangeException(nameof(pItems), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
        }

        public cSequenceSet(uint pNumber) : base(ZFromNumber(pNumber)) { }

        public cSequenceSet(uint pFrom, uint pTo) : base(ZFromRange(pFrom, pTo)) { }

        public bool Includes(uint pNumber, uint pAsterisk)
        {
            if (pNumber == 0) throw new ArgumentOutOfRangeException(nameof(pNumber));
            foreach (var lItem in this) if (lItem.Includes(pNumber, pAsterisk)) return true;
            return false;
        }

        public string ToCompactString()
        {
            var lBuilder = new StringBuilder();
            bool lFirst = true;
            foreach (var lItem in this)
            {
                if (lFirst) lFirst = false;
                else lBuilder.Append(",");
                lBuilder.Append(lItem.ToCompactString());
            }
            return lBuilder.ToString();
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

            lItems[0] = new cSequenceSetNumber(pNumber);

            return lItems;
        }

        private static cSequenceSetItem[] ZFromRange(uint pFrom, uint pTo)
        {
            if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
            if (pFrom > pTo) throw new ArgumentOutOfRangeException(nameof(pTo));

            cSequenceSetItem[] lItems = new cSequenceSetItem[1];

            lItems[0] = new cSequenceSetRange(new cSequenceSetNumber(pFrom), new cSequenceSetNumber(pTo));

            return lItems;
        }
    }

    public abstract class cSequenceSetItem
    {
        public static readonly cSequenceSetRangePart Asterisk = new cAsterisk();

        public abstract bool Includes(uint pNumber, uint pAsterisk);
        public abstract bool TryExpand(uint pAsterisk, out cUIntList rResult);
        public abstract string ToCompactString();

        private class cAsterisk : cSequenceSetRangePart
        {
            public cAsterisk() { }

            public override bool TryGetValue(uint pAsterisk, out uint rResult)
            {
                rResult = pAsterisk;
                return pAsterisk != 0;
            }

            public override uint Value(uint pAsterisk)
            {
                if (pAsterisk == 0) throw new ArgumentOutOfRangeException(nameof(pAsterisk));
                return pAsterisk;
            }

            public override int CompareTo(cSequenceSetRangePart pOther)
            {
                if (pOther == null) throw new ArgumentOutOfRangeException(nameof(pOther));
                if (pOther == Asterisk) return 0;
                return 1;
            }

            public override string ToCompactString() => "*";

            public override string ToString() => $"{nameof(cAsterisk)}()";
        }
    }

    public abstract class cSequenceSetRangePart : cSequenceSetItem, IComparable<cSequenceSetRangePart>
    {
        public sealed override bool TryExpand(uint pAsterisk, out cUIntList rResult)
        {
            if (!TryGetValue(pAsterisk, out var lValue)) { rResult = null; return false; }
            rResult = cUIntList.FromUInt(lValue);
            return true;
        }

        public override bool Includes(uint pNumber, uint pAsterisk) => Value(pAsterisk) == pNumber;

        public abstract uint Value(uint pAsterisk);
        public abstract bool TryGetValue(uint pAsterisk, out uint rValue);
        public abstract int CompareTo(cSequenceSetRangePart pOther);
    }

    public class cSequenceSetNumber : cSequenceSetRangePart
    {
        private readonly uint mNumber;

        public cSequenceSetNumber(uint pNumber)
        {
            if (pNumber == 0) throw new ArgumentOutOfRangeException(nameof(pNumber));
            mNumber = pNumber;
        }

        public override bool TryGetValue(uint pAsterisk, out uint rResult)
        {
            rResult = mNumber;
            return true;
        }

        public override uint Value(uint pAsterisk) => mNumber;

        public override int CompareTo(cSequenceSetRangePart pOther)
        {
            if (pOther == Asterisk) return -1;
            if (!(pOther is cSequenceSetNumber lOther)) throw new ArgumentOutOfRangeException(nameof(pOther));
            return mNumber.CompareTo(lOther.mNumber);
        }

        public override string ToCompactString() => mNumber.ToString();

        public override string ToString() => $"{nameof(cSequenceSetNumber)}({mNumber})";
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
            if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
            if (pTo <= pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));
            From = new cSequenceSetNumber(pFrom);
            To = new cSequenceSetNumber(pTo);
        }

        public override bool Includes(uint pNumber, uint pAsterisk)
        {
            var lLeft = From.Value(pAsterisk);
            var lRight = To.Value(pAsterisk);
            if (lLeft > lRight) return pNumber >= lRight && pNumber <= lLeft;
            return pNumber >= lLeft && pNumber <= lRight;
        }

        public override bool TryExpand(uint pAsterisk, out cUIntList rResult)
        {
            if (!From.TryGetValue(pAsterisk, out var lLeft) || !To.TryGetValue(pAsterisk, out var lRight)) { rResult = null; return false; }

            uint lFrom;
            uint lTo;

            if (lLeft > lRight)
            {
                lFrom = lRight;
                lTo = lLeft;                
            }
            else
            {
                lFrom = lLeft;
                lTo = lRight;
            }

            rResult = new cUIntList();

            for (uint i = lFrom; i <= lTo; i++) rResult.Add(i);

            return true;
        }

        public override string ToCompactString() => $"{From.ToCompactString()}:{To.ToCompactString()}";

        public override string ToString() => $"{nameof(cSequenceSetRange)}({From},{To})";
    }
}