using System;
using System.Threading;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cSectionCache
    {
        internal sealed class cAccessor : IDisposable
        {
            private bool mDisposed = false;
            private readonly cSectionCache mCache;
            private readonly cTrace.cContext mContext;
            private int mCount = 1;

            public cAccessor(cSectionCache pCache, cTrace.cContext pParentContext)
            {
                mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                mContext = pParentContext.NewObject(nameof(cAccessor), pCache);
            }

            public bool TryGetReader(cSectionCachePersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(TryGetReader), pKey);
                return mCache.ZTryGetReader(pKey, out rReader, lContext);
            }

            public bool TryGetReader(cSectionCacheNonPersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(TryGetReader), pKey);
                return mCache.ZTryGetReader(pKey, out rReader, lContext);
            }

            public cItem.cReaderWriter GetReaderWriter(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(GetReaderWriter), pKey);
                return mCache.ZGetReaderWriter(pKey, lContext);
            }

            public cItem.cReaderWriter GetReaderWriter(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(GetReaderWriter), pKey);
                return mCache.ZGetReaderWriter(pKey, lContext);
            }

            public void Dispose()
            {
                if (mDisposed) return;
                if (Interlocked.Decrement(ref mCount) == 0) mCache.ZDecrementOpenAccessorCount(mContext);
                mDisposed = true;
            }
        }
    }
}
