using System;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal class cNoUIDFlagCacheItem : iFlagCacheItem
    {
        private cModSeqFlags mModSeqFlags = null;

        public fMessageCacheAttributes Attributes => mModSeqFlags == null ? 0 : fMessageCacheAttributes.modseqflags;

        public cModSeqFlags ModSeqFlags => mModSeqFlags;

        public void Update(iFlagDataItem pFlagDataItem, cTrace.cContext pParentContext)
        {
            // note that in a normal flagcacheitem you would have to defend against concurrent updates as well 
            //  (we don't have to do that here because instances of this class cannot be shared by two clients)
            var lContext = pParentContext.NewMethod(nameof(cNoUIDFlagCacheItem), nameof(Update), pFlagDataItem);
            if (pFlagDataItem.ModSeqFlags == null) return;
            if (mModSeqFlags == null || pFlagDataItem.ModSeqFlags.ModSeq == 0 || pFlagDataItem.ModSeqFlags.ModSeq > mModSeqFlags.ModSeq) mModSeqFlags = pFlagDataItem.ModSeqFlags;
        }
    }
}