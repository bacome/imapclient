using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private static cSectionCache mGlobalSectionCache = new cTempFileSectionCache("cIMAPClient.GlobalSectionCache", 1000, 100000000, 60000, new cBatchSizerConfiguration(1000, 100000, 1000, 1000));

        public static cSectionCache GlobalSectionCache
        {
            get => mGlobalSectionCache;
            set => mGlobalSectionCache = value ?? throw new ArgumentNullException();
        }

        private readonly object mSectionCacheLock = new object();
        private bool mSectionCacheDisposing = false;
        private cSectionCache mSectionCache = null;
        private cSectionCache.cAccessor mSectionCacheAccessor = null;

        public cSectionCache SectionCache
        {
            get => mSectionCache;

            set
            {
                lock (mSectionCacheLock)
                {
                    if (mSectionCacheAccessor != null)
                    {
                        mSectionCacheAccessor.Dispose();
                        mSectionCacheAccessor = null;
                    }

                    mSectionCache = value;
                }
            }
        }

        internal bool TryGetSectionCacheItemReader(cSectionCachePersistentKey pKey, out cSectionCache.cItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(TryGetSectionCacheItemReader), pKey);
            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
            var lAccessor = ZGetSectionCacheAccessor(lContext);
            return lAccessor.TryGetItemReader(pKey, out rReader, lContext);
        }

        internal bool TryGetSectionCacheItemReader(cSectionCache.cNonPersistentKey pKey, out cSectionCache.cItem.cReader rReader, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(TryGetSectionCacheItemReader), pKey);
            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
            var lAccessor = ZGetSectionCacheAccessor(lContext);
            return lAccessor.TryGetItemReader(pKey, out rReader, lContext);
        }

        internal cSectionCache.cItem.cReaderWriter GetSectionCacheItemReaderWriter(cSectionCachePersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetSectionCacheItemReaderWriter), pKey);
            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
            var lAccessor = ZGetSectionCacheAccessor(lContext);
            return lAccessor.GetItemReaderWriter(pKey, lContext);
        }

        internal cSectionCache.cItem.cReaderWriter GetSectionCacheItemReaderWriter(cSectionCache.cNonPersistentKey pKey, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(GetSectionCacheItemReaderWriter), pKey);
            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
            var lAccessor = ZGetSectionCacheAccessor(lContext);
            return lAccessor.GetItemReaderWriter(pKey, lContext);
        }

        private cSectionCache.cAccessor ZGetSectionCacheAccessor(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZGetSectionCacheAccessor));
            
            lock (mSectionCacheLock)
            {
                if (mSectionCacheAccessor != null) return mSectionCacheAccessor;
                if (mSectionCacheDisposing) throw new ObjectDisposedException(nameof(cIMAPClient));
                if (mSectionCache == null) mSectionCacheAccessor = mGlobalSectionCache.GetAccessor(lContext);
                else mSectionCacheAccessor = mSectionCache.GetAccessor(lContext);
                return mSectionCacheAccessor;
            }
        }

        private void ZDisposeSectionCache()
        {
            lock (mSectionCacheLock)
            {
                mSectionCacheDisposing = true;
            }

            if (mSectionCacheAccessor != null)
            {
                try { mSectionCacheAccessor.Dispose(); }
                catch { }
            }
        }
    }
}