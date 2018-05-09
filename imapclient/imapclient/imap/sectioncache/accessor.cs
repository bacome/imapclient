using System;
using System.Threading;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCache
    {
        internal sealed class cAccessor : IDisposable
        {
            private bool mDisposed = false;
            private readonly cSectionCache mCache;
            private readonly Action<cTrace.cContext> mDecrementOpenAccessorCount;
            private readonly cTrace.cContext mContextToUseWhenDisposing;
            private int mCount = 1;

            public cAccessor(cSectionCache pCache, Action<cTrace.cContext> pDecrementOpenAccessorCount, cTrace.cContext pContextToUseWhenDisposing)
            {
                mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                mDecrementOpenAccessorCount = pDecrementOpenAccessorCount ?? throw new ArgumentNullException(nameof(pDecrementOpenAccessorCount));
                mContextToUseWhenDisposing = pContextToUseWhenDisposing;
            }

            public bool TryGetItemReader(cSectionCachePersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext) => mCache.ZTryGetItemReader(pKey, out rReader, pParentContext);
            public bool TryGetItemReader(cSectionCacheNonPersistentKey pKey, out cSectionCacheItemReader rReader, cTrace.cContext pParentContext) => mCache.ZTryGetItemReader(pKey, out rReader, pParentContext);
            public cSectionCacheItem GetNewItem(cTrace.cContext pParentContext) => mCache.ZGetNewItem(pParentContext);
            public void AddItem(cSectionCachePersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext) => mCache.ZAddItem(pKey, pItem, pParentContext);
            public void AddItem(cSectionCacheNonPersistentKey pKey, cSectionCacheItem pItem, cTrace.cContext pParentContext) => mCache.ZAddItem(pKey, pItem, pParentContext);

            public void Dispose()
            {
                if (mDisposed) return;
                if (Interlocked.Decrement(ref mCount) == 0) mDecrementOpenAccessorCount(mContextToUseWhenDisposing);
                mDisposed = true;
            }
        }
    }
}
