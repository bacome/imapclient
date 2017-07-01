using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHandleList : List<iMessageHandle>
    {
        public cHandleList() { }
        public cHandleList(iMessageHandle pHandle) : base(new iMessageHandle[] { pHandle }) { }
        public cHandleList(IList<iMessageHandle> pHandles) : base(pHandles) { }

        public void SortByCacheSequence() => Sort(ZCompareCacheSequence);
        public void Sort(cSort pSort) => Sort(pSort.Comparison);

        private static int ZCompareCacheSequence(iMessageHandle pX, iMessageHandle pY) => pX.CacheSequence.CompareTo(pY.CacheSequence);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHandleList));

            object lLastCache = null;

            foreach (var lHandle in this)
            {
                if (!ReferenceEquals(lHandle.Cache, lLastCache))
                {
                    lLastCache = lHandle.Cache;
                    lBuilder.Append(lHandle.Cache);
                }

                lBuilder.Append(lHandle.CacheSequence);
            }

            return lBuilder.ToString();
        }
    }
}