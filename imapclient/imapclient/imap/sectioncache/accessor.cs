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

            public bool TryGetItemReader(cSectionCachePersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(TryGetItemReader), pKey);
                return mCache.ZTryGetItemReader(pKey, out rReader, lContext);
            }

            public bool TryGetItemReader(cNonPersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(TryGetItemReader), pKey);
                return mCache.ZTryGetItemReader(pKey, out rReader, lContext);
            }

            public cItem.cReaderWriter GetItemReaderWriter(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(GetItemReaderWriter));
                return mCache.ZGetItemReaderWriter(lContext);
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
