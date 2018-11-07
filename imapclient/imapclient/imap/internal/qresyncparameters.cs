using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cQResyncParameters
        {
            public readonly uint CachedUIDValidity;
            public readonly ulong CachedHighestModSeq;
            public readonly cSequenceSet CachedUIDs; // not null
            public readonly Action<int> Increment; // can be null

            public cQResyncParameters(uint pCachedUIDValidity, ulong pCachedHighestModSeq, HashSet<cUID> pCachedUIDs, int pMaxItemsInSequenceSet, Action<int> pIncrement)
            {
                if (pCachedUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pCachedUIDValidity));
                CachedUIDValidity = pCachedUIDValidity;
                if (pCachedHighestModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pCachedHighestModSeq));
                CachedHighestModSeq = pCachedHighestModSeq;
                if (pCachedUIDs == null) throw new ArgumentNullException(nameof(pCachedUIDs));
                if (pCachedUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pCachedUIDs));
                CachedUIDs = cSequenceSet.FromUInts(from lUID in pCachedUIDs select lUID.UID, pMaxItemsInSequenceSet);
                Increment = pIncrement;
            }
        }
    }
}