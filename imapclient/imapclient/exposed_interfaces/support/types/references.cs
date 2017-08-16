using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    public class cReferences
    {
        public static readonly cReferences None = new cReferences();

        public readonly uint? UIDValidity;
        public readonly ReadOnlyCollection<iMessageHandle> Handles;

        private cReferences()
        {
            UIDValidity = null;
            Handles = null;
        }

        private cReferences(ReadOnlyCollection<iMessageHandle> pHandles)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pHandles.Count < 2) throw new ArgumentOutOfRangeException(nameof(pHandles));
            UIDValidity = pHandles[0].Cache.UIDValidity;
            Handles = pHandles;
        }

        public cReferences(iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            UIDValidity = pHandle.Cache.UIDValidity;
            List<iMessageHandle> lList = new List<iMessageHandle>();
            lList.Add(pHandle);
            Handles = lList.AsReadOnly();
        }

        public cReferences(uint pUIDValidity)
        {
            UIDValidity = pUIDValidity;
            Handles = null;
        }

        public cReferences Combine(cReferences pReferences)
        {
            if (pReferences == null) throw new ArgumentNullException(nameof(pReferences));

            if (UIDValidity != null && pReferences.UIDValidity != null && UIDValidity != pReferences.UIDValidity) throw new ArgumentOutOfRangeException(nameof(pReferences), "inconsistent uidvalidity");
            if (Handles != null && pReferences.Handles != null && !ReferenceEquals(Handles[0].Cache, pReferences.Handles[0].Cache)) throw new ArgumentOutOfRangeException(nameof(pReferences), "inconsistent message cache");

            if (Handles == null && pReferences.Handles == null)
            {
                if (pReferences.UIDValidity != null) return pReferences;
                return this;
            }

            if (Handles == null) return pReferences;
            if (pReferences.Handles == null) return this;

            List<iMessageHandle> lList = new List<iMessageHandle>(Handles);
            lList.AddRange(pReferences.Handles);

            return new cReferences(lList.AsReadOnly());
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cReferences));

            if (Handles != null)
            {
                lBuilder.Append(Handles[0].Cache);
                foreach (var lHandle in Handles) lBuilder.Append(lHandle.CacheSequence);
            }
            else lBuilder.Append(UIDValidity);

            return lBuilder.ToString();
        }
    }
}
