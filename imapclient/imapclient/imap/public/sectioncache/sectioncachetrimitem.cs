using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheTrimItem : IComparable<cSectionCacheTrimItem>
    {
        private readonly cSectionCache.cItem mItem;
        private readonly int mChangeSequence;
        private readonly IComparable mSortParameters;

        internal cSectionCacheTrimItem(cSectionCache.cItem pItem, bool pZero)
        {
            mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
            if (pZero) mChangeSequence = 0;
            else mChangeSequence = pItem.ChangeSequence;
            mSortParameters = pItem.GetSortParameters() ?? throw new cUnexpectedSectionCacheActionException();
        }

        // for creating an instance to do a list.binarysearch
        public cSectionCacheTrimItem(IComparable pSortParameters)
        {
            mItem = null;
            mChangeSequence = 0;
            mSortParameters = pSortParameters;
        }

        public void TryDelete(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheTrimItem), nameof(TryDelete));
            if (mItem == null) throw new InvalidOperationException();
            mItem.TryDelete(mChangeSequence, lContext);
        }

        public int CompareTo(cSectionCacheTrimItem pOther) => mSortParameters.CompareTo(pOther.mSortParameters);
    }
}