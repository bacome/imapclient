using System;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cFileBasedSectionCacheItem : cSectionCacheItem
    {
        private FileInfo mFileInfo;
        private long mLength;

        protected internal cFileBasedSectionCacheItem(cFileBasedSectionCache pCache, FileInfo pFileInfo) : base(pCache, pFileInfo.FullName)
        {
            mFileInfo = pFileInfo;
            mLength = mFileInfo.Length;
        }

        protected internal cFileBasedSectionCacheItem(cFileBasedSectionCache pCache, string pFileName, Stream pReadWriteStream) : base(pCache, pFileName, pReadWriteStream)
        {
            mFileInfo = null;
            mLength = -1;
        }

        sealed protected override Stream YGetReadStream(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCacheItem), nameof(YGetReadStream));
            var lStream = new FileStream(ItemKey, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (lStream.Length == mLength) return lStream; // still a risk that it isn't the right file
            ;?; // check the datetime also
            lStream.Dispose();
            SetDeleted(lContext);
            return null;
        }

        // override to delete the index file as well
        protected override void YDelete(cTrace.cContext pParentContext) => File.Delete(ItemKey);

        // override to write the index file (_after_ doing this)
        protected override void ItemEncached(long pLength, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCacheItem), nameof(ItemEncached));
            mFileInfo = new FileInfo(ItemKey);
            mLength = pLength;
            ((cFileBasedSectionCache)Cache).ItemEncached(this, pLength, lContext);
        }

        protected override void ItemDecached(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCacheItem), nameof(ItemDecached));
            ((cFileBasedSectionCache)Cache).ItemDecached(mLength, lContext);
        }

        protected override void Touch(cTrace.cContext pParentContext) => mFileInfo.LastAccessTimeUtc = DateTime.UtcNow;

        protected internal DateTime LastAccessTimeUtc => mFileInfo.LastAccessTimeUtc;
    }
}