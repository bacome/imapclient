using System;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal class cDefaultSectionCache : cSectionCache
    {
        public cDefaultSectionCache() : base(nameof(cDefaultSectionCache), 60000)
        {
            StartMaintenance();
        }

        protected override cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDefaultSectionCache), nameof(YGetNewItem));

            string lFullName = Path.GetTempFileName();
            Stream lStream = new FileStream(lFullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            var lFileInfo = new FileInfo(lFullName);
            var lItem = new cItem(this, lFullName, lStream, lFileInfo.CreationTimeUtc);

            return lItem;
        }

        private class cItem : cSectionCacheItem
        {
            private DateTime mCreationTimeUTC;

            public cItem(cDefaultSectionCache pCache, string pFullName, Stream pReadWriteStream, DateTime pCreationTimeUTC) : base(pCache, pFullName, pReadWriteStream)
            {
                mCreationTimeUTC = pCreationTimeUTC;
            }

            protected override Stream YGetReadStream(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(YGetReadStream));
                var lStream = new FileStream(ItemKey, FileMode.Open, FileAccess.Read, FileShare.Read);
                var lFileInfo = new FileInfo(ItemKey);
                if (FileTimesAreTheSame(lFileInfo.CreationTimeUtc, mCreationTimeUTC)) return lStream; // length is checked by the cache
                lStream.Dispose();
                return null;
            }

            protected override void YDelete(cTrace.cContext pParentContext) => File.Delete(ItemKey);

            protected override eItemState Touch(cTrace.cContext pParentContext)
            {
                File.Delete(ItemKey);
                return eItemState.deleted;
            }
        }
    }
}