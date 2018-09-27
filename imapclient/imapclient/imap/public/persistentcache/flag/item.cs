using System;
using System.Runtime.Serialization;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFlagCacheItem : cPersistentCacheItem
    {
        protected internal cFlagCacheItem(cFlagCache pCache, cFlagCacheItemData pData) : base(pCache, pData) { }

        // note that the cache for the containing mailbox has been updated without a modseq being specified 
        //  => it cannot claim to be synchronised up to any highestmodseq
        //
        ;?;  protected abstract void YSetNoModSeq(cTrace.cContext pParentContext);

        public fMessageCacheAttributes Attributes => ((cFlagCacheItemData)mData).Attributes;

        public cModSeqFlags ModSeqFlags
        {
            get
            {
                var lValue = ((cFlagCacheItemData)mData).ModSeqFlags;
                if (lValue == null) return null;
                RecordAccess();
                return lValue;
            }

            internal set
            {
                if (value == null) throw new ArgumentNullException();
                if (value.ModSeq == 0) ((cFlagCache)mCache). <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< GOT TO CLEARHIGHESTMODSEQ
            }
        }



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

    [Serializable]
    public class cFlagCacheItemData : cPersistentCacheItemData
    {
        private cModSeqFlags mModSeqFlags;

        internal cFlagCacheItemData()
        {
            mModSeqFlags = null;
        }

        public fMessageCacheAttributes Attributes => mModSeqFlags == null ? 0 : fMessageCacheAttributes.flags;

        public cModSeqFlags ModSeqFlags
        {
            get => mModSeqFlags;

            internal set
            {
                if (value == null) throw new ArgumentNullException();

                lock (mUpdateLock)
                {
                    if (mModSeqFlags != null && value.ModSeq != 0 && value.ModSeq <= mModSeqFlags.ModSeq) return;
                    mModSeqFlags = value;
                }
            }
        }
    }
}
