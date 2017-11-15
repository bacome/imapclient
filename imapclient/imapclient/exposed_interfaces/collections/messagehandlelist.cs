using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A list of messages in the internal message cache.
    /// </summary>
    public class cMessageHandleList : List<iMessageHandle>
    {
        internal cMessageHandleList() { }
        internal cMessageHandleList(IEnumerable<iMessageHandle> pHandles) : base(pHandles) { }

        internal void SortByCacheSequence() => Sort(ZCompareCacheSequence);

        /**<summary>Returns a string that represents the list.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageHandleList));

            object lLastCache = null;

            foreach (var lHandle in this)
            {
                if (lHandle == null) lBuilder.Append(lHandle);
                else
                {
                    if (!ReferenceEquals(lHandle.Cache, lLastCache))
                    {
                        lLastCache = lHandle.Cache;
                        lBuilder.Append(lHandle.Cache);
                    }

                    lBuilder.Append(lHandle.CacheSequence);
                }
            }

            return lBuilder.ToString();
        }

        private static int ZCompareCacheSequence(iMessageHandle pX, iMessageHandle pY)
        {
            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            return pX.CacheSequence.CompareTo(pY.CacheSequence);
        }

        internal static cMessageHandleList FromHandle(iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            var lResult = new cMessageHandleList();
            lResult.Add(pHandle);
            return lResult;
        }

        internal static cMessageHandleList FromHandles(IEnumerable<iMessageHandle> pHandles)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));

            object lCache = null;

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains nulls");
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed caches");
            }

            return new cMessageHandleList(pHandles.Distinct());
        }

        internal static cMessageHandleList FromMessages(IEnumerable<cMessage> pMessages)
        {
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            object lCache = null;
            cMessageHandleList lHandles = new cMessageHandleList();

            foreach (var lMessage in pMessages)
            {
                if (lMessage == null) throw new ArgumentOutOfRangeException(nameof(pMessages), "contains nulls");
                if (lCache == null) lCache = lMessage.Handle.Cache;
                else if (!ReferenceEquals(lMessage.Handle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pMessages), "contains mixed caches");
                lHandles.Add(lMessage.Handle);
            }

            return new cMessageHandleList(lHandles.Distinct());
        }
    }
}