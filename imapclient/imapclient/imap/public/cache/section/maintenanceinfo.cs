using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cSectionCacheMaintenanceData
    {
        public readonly HashSet<cMessageUID> Expunged;
        public readonly ReadOnlyDictionary<cMailboxId, uint> UIDValiditiesDiscovered;

        internal cSectionCacheMaintenanceData(IList<cMessageUID> pExpungedMessages, IDictionary<cMailboxId, uint> pUIDValidities)
        {
            ExpungedMessages = new ReadOnlyCollection<cMessageUID>(pExpungedMessages);
            UIDValidities = new ReadOnlyDictionary<cMailboxId, uint>(pUIDValidities);
        }





        /// <inheritdoc />
        public override string ToString()
        {
            ;?;
        }
    }

}