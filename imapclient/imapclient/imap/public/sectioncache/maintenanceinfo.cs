using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheMaintenanceInfo
    {
        public readonly HashSet<cSectionCacheMessageId> Expunged;
        public readonly ReadOnlyDictionary<cSectionCacheMailboxId, uint> UIDValiditiesDiscovered;

        internal cSectionCacheMaintenanceInfo(IList<cSectionCacheMessageId> pExpungedMessages, IDictionary<cSectionCacheMailboxId, uint> pUIDValidities)
        {
            ExpungedMessages = new ReadOnlyCollection<cSectionCacheMessageId>(pExpungedMessages);
            UIDValidities = new ReadOnlyDictionary<cSectionCacheMailboxId, uint>(pUIDValidities);
        }





        /// <inheritdoc />
        public override string ToString()
        {
            ;?;
        }
    }

}