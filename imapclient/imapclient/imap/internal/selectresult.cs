using System;
using System.Collections.Generic;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal class cSelectResult
    {
        public readonly uint UIDValidity; // the selected mailbox's UIDValidity when selected
        //public readonly bool UIDNotSticky; //  ditto
        public readonly ulong CachedHighestModSeq; // the persistent cache's highestmodseq for the mailbox from before the mailbox was selected IF the UIDValidity matches AND the reported current hms was NOT less than this value
        public readonly HashSet<cUID> QResyncedUIDs; // the UIDs that were used in qresync (IF the above conditions are true)
        public readonly Action<cTrace.cContext> SetCallSetHighestModSeq; // the callback to enable sending of highestmodseqs to the persistent cache (called after the cache is known to be in sync)

        public cSelectResult(uint pUIDValidity, bool pUIDNotSticky, ulong pCachedHighestModSeq, HashSet<cUID> pQResyncedUIDs, Action<cTrace.cContext> pSetCallSetHighestModSeq)
        {
            UIDValidity = pUIDValidity; 
            UIDNotSticky = pUIDNotSticky;
            CachedHighestModSeq = pCachedHighestModSeq;
            QResyncedUIDs = pQResyncedUIDs;
            SetCallSetHighestModSeq = pSetCallSetHighestModSeq ?? throw new ArgumentNullException(nameof(pSetCallSetHighestModSeq));
        }
    }
}
