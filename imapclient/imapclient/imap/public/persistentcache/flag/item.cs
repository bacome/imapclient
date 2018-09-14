using System;

namespace work.bacome.imapclient
{
    public abstract class cFlagCacheItem
    {
        public ulong? ModSeq { get; }
        public cFetchableFlags Flags { get; }
    }
}
