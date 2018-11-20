using System;
using System.Collections.Generic;

namespace work.bacome.imapinternals
{
    public partial class cSequenceSet
    {
        public class cLimitingFactory
        {
            // generates sequence sets to just over the specified limitsizeinbytes
            //  (for controlling line length in fetchcacheitems, copy and store)

            private readonly int mASCIILengthLimit;
            private bool mLimitReached = false;
            private bool mComplete = false;

            private bool mFirst = true;
            private uint mFrom = 0;
            private uint mTo = 0;
            private int mASCIILength = 0;
            private List<cSequenceSetItem> mItems = new List<cSequenceSetItem>();

            public cLimitingFactory(int pASCIILengthLimit)
            {
                mASCIILengthLimit = pASCIILengthLimit;
            }

            public bool Add(uint pUInt)
            {
                // returns true if the number was added, otherwise false
                //  if false is returned, no more numbers can be added

                if (mLimitReached || mComplete) throw new InvalidOperationException();
                if (pUInt == 0) throw new ArgumentOutOfRangeException(nameof(pUInt));
                if (pUInt <= mTo) throw new InvalidOperationException();

                if (mFirst)
                {
                    mFirst = false;
                    mFrom = pUInt;
                    mTo = pUInt;
                    return true; // was added
                }

                if (pUInt == mTo + 1)
                {
                    mTo = pUInt;
                    return true; // was added
                }

                ZAddItem();

                if (mASCIILength >= mASCIILengthLimit)
                {
                    mLimitReached = true;
                    return false; // not added
                }

                mFrom = pUInt;
                mTo = pUInt;
                return true; // was added
            }

            public cSequenceSet SequenceSet
            {
                get
                {
                    if (mFirst) throw new InvalidOperationException();
                    if (mComplete) throw new InvalidOperationException();
                    if (!mLimitReached) ZAddItem();
                    mComplete = true;
                    return new cSequenceSet(mItems);
                }
            }

            private void ZAddItem()
            {
                if (mItems.Count > 0) mASCIILength++; // comma
                mASCIILength += ZASCIILength(mFrom);

                if (mFrom == mTo) mItems.Add(new cSequenceSetNumber(mFrom));
                else
                {
                    mASCIILength += ZASCIILength(mTo) + 1; // the extra one is for the colon
                    mItems.Add(new cSequenceSetRange(mFrom, mTo));
                }
            }
        }
    }
}
