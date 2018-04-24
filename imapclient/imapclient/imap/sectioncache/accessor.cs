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

            public bool TryGetReader(cSectionCacheNonPersistentKey pKey, out cItem.cReader rReader, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cAccessor), nameof(ZTryGetReader), pKey);
                return mCache.ZTryGetReader(pKey, out rReader, lContext);
            }

            private cItem.cReaderWriter ZGetReaderWriter(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetReaderWriter), pKey);

                lock (mLock)
                {
                    var lItem = GetNewItem();
                    if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
                    return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
                }
            }

            private cItem.cReaderWriter ZGetReaderWriter(cSectionCacheNonPersistentKey pKey, cTrace.cContext pParentContext)
            {
                // if the uid is available the other one should have been called

                var lContext = pParentContext.NewMethod(nameof(cSectionCache), nameof(ZGetReaderWriter), pKey);

                lock (mLock)
                {
                    var lItem = GetNewItem();
                    if (lItem == null || !lItem.CanWrite) throw new cUnexpectedSectionCacheActionException(lContext);
                    return lItem.GetReaderWriter(pKey, mWriteSizer, lContext);
                }
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
