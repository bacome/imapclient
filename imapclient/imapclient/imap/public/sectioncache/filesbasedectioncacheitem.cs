using System;
using System.IO;
using System.Threading;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{

    public class cFileBasedSectionCacheItem : cSectionCacheItem
    {
        private readonly string mFileName;
        private FileInfo mFileInfo;
        private long mAccountingLength;
        ;?;
        private DateTime mSnapshotLastAccessTime;

        protected internal cFileBasedSectionCacheItem(cSectionCache pCache, FileInfo pFileInfo) : base(pCache)
        {
            mFileName = pFileInfo.FullName;
            mFileInfo = pFileInfo;
            mAccountingLength = mFileInfo.Length;
        }

        protected internal cFileBasedSectionCacheItem(cSectionCache pCache, string pFileName, Stream pReadWriteStream) : base(pCache, pReadWriteStream)
        {
            mFileName = pFileName;
            mFileInfo = null;
            mAccountingLength = -1;
        }

        private FileInfo FileInfo
        {
            get
            {
                if (mFileInfo == null) mFileInfo = new FileInfo(mFileName);
                return mFileInfo;
            }
        }

        protected internal long AccountingLength
        {
            get
            {
                if (mAccountingLength == -1)
                {
                    var lAccountingLength = FileInfo.Length;
                    Interlocked.CompareExchange(ref mAccountingLength, lAccountingLength, -1);
                }

                return mAccountingLength;
            }
        }

        sealed protected internal override object ItemKey => mFileName;

        protected internal override bool PersistentKeyAssigned => false;

        sealed protected override Stream GetReadStream(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFileBasedSectionCacheItem), nameof(GetReadStream));
            var lStream = new FileStream(mFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (lStream.Length == mAccountingLength) return lStream; // still a risk that it isn't the right file
            lStream.Dispose();
            SetDeleted(lContext);
            return null;
        }

        protected override void Touch(cTrace.cContext pParentContext) => FileInfo.LastAccessTimeUtc = DateTime.UtcNow;

        sealed protected override void Delete(cTrace.cContext pParentContext) => File.Delete(mFileName);

        internal void SnapshotLastAccessTimeForSort()
        {
            mSnapshotLastAccessTime = FileInfo.LastAccessTimeUtc;
        }

        public int CompareTo(cFileBasedSectionCacheItem pOther)
        {
            if (pOther == null) return 1;
            return mSnapshotLastAccessTime.CompareTo(pOther.mSnapshotLastAccessTime);
        }

        public override string ToString() => $"{nameof(cFileBasedSectionCacheItem)}({mFileName})";
    }
}