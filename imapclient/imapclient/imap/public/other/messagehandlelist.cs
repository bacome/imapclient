using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A list of messages.
    /// </summary>
    public class cMessageHandleList : List<iMessageHandle>
    {
        internal cMessageHandleList() { }
        internal cMessageHandleList(IEnumerable<iMessageHandle> pMessageHandles) : base(pMessageHandles) { }

        internal void SortByCacheSequence() => Sort(ZCompareCacheSequence);

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageHandleList));

            object lLastCache = null;

            foreach (var lMessageHandle in this)
            {
                if (lMessageHandle == null) lBuilder.Append(lMessageHandle);
                else
                {
                    if (!ReferenceEquals(lMessageHandle.MessageCache, lLastCache))
                    {
                        lLastCache = lMessageHandle.MessageCache;
                        lBuilder.Append(lMessageHandle.MessageCache);
                    }

                    lBuilder.Append(lMessageHandle.CacheSequence);
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

        internal static cMessageHandleList FromMessageHandle(iMessageHandle pMessageHandle)
        {
            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            var lResult = new cMessageHandleList();
            lResult.Add(pMessageHandle);
            return lResult;
        }

        internal static cMessageHandleList FromMessageHandles(IEnumerable<iMessageHandle> pMessageHandles)
        {
            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));

            object lMessageCache = null;

            foreach (var lMessageHandle in pMessageHandles)
            {
                if (lMessageHandle == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandles), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (lMessageCache == null) lMessageCache = lMessageHandle.MessageCache;
                else if (!ReferenceEquals(lMessageHandle.MessageCache, lMessageCache)) throw new ArgumentOutOfRangeException(nameof(pMessageHandles), kArgumentOutOfRangeExceptionMessage.ContainsMixedMessageCaches);
            }

            return new cMessageHandleList(pMessageHandles.Distinct());
        }

        internal static cMessageHandleList FromMessages(IEnumerable<cIMAPMessage> pMessages)
        {
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            object lMessageCache = null;
            cMessageHandleList lMessageHandles = new cMessageHandleList();

            foreach (var lMessage in pMessages)
            {
                if (lMessage == null) throw new ArgumentOutOfRangeException(nameof(pMessages), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (lMessageCache == null) lMessageCache = lMessage.MessageHandle.MessageCache;
                else if (!ReferenceEquals(lMessage.MessageHandle.MessageCache, lMessageCache)) throw new ArgumentOutOfRangeException(nameof(pMessages), kArgumentOutOfRangeExceptionMessage.ContainsMixedMessageCaches);
                lMessageHandles.Add(lMessage.MessageHandle);
            }

            return new cMessageHandleList(lMessageHandles.Distinct());
        }
    }
}