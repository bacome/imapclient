using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.mailclient
{
    public class cMessageData
    {
        public readonly fMessageDataFormat Format = 0;
        public readonly ReadOnlyCollection<cMessageDataPart> Parts;

        public cMessageData(IEnumerable<cMessageDataPart> pParts)
        {
            if (pParts == null) throw new ArgumentNullException(nameof(pParts));

            var lParts = new List<cMessageDataPart>();

            bool lHasContent = false;

            foreach (var lPart in pParts)
            {
                if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                Format |= lPart.Format;
                if (lPart.HasContent) lHasContent = true;
                lParts.Add(lPart);
            }

            if (!lHasContent) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.HasNoContent);

            Parts = lParts.AsReadOnly();
        }
    }
}