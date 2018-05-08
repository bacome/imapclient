using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheItemSnapshot
    {
        private readonly cSectionCacheItem mItem;
        private readonly int mChangeSequence;

        internal cSectionCacheItemSnapshot(cSectionCacheItem pItem, bool pZero)
        {
            mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
            if (pZero) mChangeSequence = 0;
            else mChangeSequence = pItem.ChangeSequence;
        }

        public void TryDelete(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSectionCacheItemSnapshot), nameof(TryDelete));
            mItem.TryDelete(mChangeSequence, lContext);
        }
    }
}