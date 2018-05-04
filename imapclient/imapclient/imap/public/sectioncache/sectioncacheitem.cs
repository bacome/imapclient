using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheItem
    {
        private readonly cSectionCache.cItem mItem;
        private readonly int mChangeSequence;

        internal cSectionCacheItem(cSectionCache.cItem pItem, bool pZero)
        {
            mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
            if (pZero) mChangeSequence = 0;
            else mChangeSequence = pItem.ChangeSequence;
        }

        public void TryDelete(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryDelete));
            mItem.TryDelete(mChangeSequence, lContext);
        }
    }
}