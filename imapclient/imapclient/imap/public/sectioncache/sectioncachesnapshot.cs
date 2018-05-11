using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheSnapshot
    {
        private readonly cSectionCache mCache;
        private readonly Dictionary<string, cItem> mItems = new Dictionary<string, cItem>();

        internal cSectionCacheSnapshot(cSectionCache pCache)
        {
            ;?;
        }

        internal void Add(cSectionCacheItem pItem)
        {
            ;?;
        }

        public void TryDelete(string pItemKey, cSectionCachePersistentKey pKey)
        {
            ;?;
        }
    }
}