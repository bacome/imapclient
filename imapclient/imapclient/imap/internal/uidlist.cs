using System;
using System.Collections.Generic;
using System.Linq;

namespace work.bacome.imapclient
{
    internal class cUIDList : List<cUID>
    {
        public cUIDList() { }
        public cUIDList(IEnumerable<cUID> pUIDs) : base(pUIDs) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIDList));
            foreach (var lUID in this) lBuilder.Append(lUID);
            return lBuilder.ToString();
        }

        public static cUIDList FromUID(cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            var lResult = new cUIDList();
            lResult.Add(pUID);
            return lResult;
        }

        public static cUIDList FromUIDs(IEnumerable<cUID> pUIDs)
        {
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            uint lUIDValidity = 0;

            foreach (var lUID in pUIDs)
            {
                if (lUID == null) throw new ArgumentOutOfRangeException(nameof(pUIDs), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (lUIDValidity == 0) lUIDValidity = lUID.UIDValidity;
                else if (lUID.UIDValidity != lUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDs), kArgumentOutOfRangeExceptionMessage.ContainsMixedUIDValidities);
            }

            return new cUIDList(pUIDs.Distinct());
        }
    }
}