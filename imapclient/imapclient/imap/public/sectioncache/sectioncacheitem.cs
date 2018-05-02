using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheItem
    {
        private readonly cSectionCache.cItem mItem;
        private readonly int mChangeSequence;
        public readonly object ItemKey;

        internal cSectionCacheItem(cSectionCache.cItem pItem, bool pZero)
        {
            mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
            if (pZero) mChangeSequence = 0;
            else mChangeSequence = pItem.ChangeSequence;
            ItemKey = pItem.GetItemKey() ?? throw new cUnexpectedSectionCacheActionException(nameof(cSectionCacheItem));
        }

        public void TryDelete(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItem), nameof(TryDelete));
            mItem.TryDelete(mChangeSequence, lContext);
        }
    }
}