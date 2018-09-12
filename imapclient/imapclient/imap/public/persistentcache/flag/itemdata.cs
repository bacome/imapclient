using System;

namespace work.bacome.imapclient
{
    // TODO: this should be serializable (which means each element should be)
    public class cFlagCacheItemData
    {
        public readonly ulong? ModSeq;
        public readonly cFetchableFlags Flags;
    }
}
