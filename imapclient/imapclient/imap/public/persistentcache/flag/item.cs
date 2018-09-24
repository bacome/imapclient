using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    ;?; // this is serilisable
    public abstract class cFlagCacheItem : cPersistentCacheItem
    {
        private readonly object mModSeqFlagsLock = new object();
        private cModSeqFlags mModSeqFlags;

        protected internal cFlagCacheItem(cFlagCache pFlagCache, long pAccessSequenceNumber, DateTime pAccessDateTime) : base(pFlagCache, pAccessSequenceNumber, pAccessDateTime) { }

        // note that the cache for the containing mailbox has been updated without a modseq being specified 
        //  => it cannot claim to be synchronised up to any highestmodseq
        //
        protected abstract void YSetNoModSeq(cTrace.cContext pParentContext); 

        public cFetchableFlags Flags
        {
            get
            {
                RecordAccess();
                return mModSeqFlags?.Flags;
            }
        }

        public ulong? ModSeq
        {
            get
            {
                RecordAccess();
                return mModSeqFlags?.ModSeq;
            }
        }

        public cModSeqFlags ModSeqFlags
        {
            get
            {
                RecordAccess();
                return mModSeqFlags;
            }
        }

        internal void SetModSeqFlags(cModSeqFlags pModSeqFlags, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cFlagCache), nameof(SetModSeqFlags), pModSeqFlags);

            if (pModSeqFlags == null) throw new ArgumentNullException(nameof(pModSeqFlags));

            lock (mModSeqFlagsLock)
            {
                if (pModSeqFlags.ModSeq == 0)
                {
                    mModSeqFlags = pModSeqFlags;
                    YSetNoModSeq(lContext);
                }
                else if (mModSeqFlags == null || mModSeqFlags.ModSeq < pModSeqFlags.ModSeq) mModSeqFlags = pModSeqFlags;
            }
        }
    }
}
