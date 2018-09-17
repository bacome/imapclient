using System;

namespace work.bacome.imapclient
{
    public interface iFlagCacheItem
    {
        cFetchableFlags Flags { get; } // nullable, but can't be set to null
        ulong? ModSeq { get; } // nullable, but can't be set to null
        void Update(cFetchableFlags pFlags, ulong pModSeq); // to allow the cache to defend against updates that revert the value to an older version
    }
}
