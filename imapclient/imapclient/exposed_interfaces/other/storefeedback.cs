using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cStoreFeedbackItemBase
    {
        public bool Fetched = false;
        public bool Modified = false;
    }

    public class cStoreFeedbackItem : cStoreFeedbackItemBase
    {
        public readonly iMessageHandle Handle;

        public cStoreFeedbackItem(iMessageHandle pHandle)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
        }

        public override string ToString() => $"{nameof(cStoreFeedbackItem)}({Handle},{Fetched},{Modified})";
    }

    public class cUIDStoreFeedbackItem : cStoreFeedbackItemBase
    {
        public readonly cUID UID;

        public cUIDStoreFeedbackItem(cUID pUID)
        {
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
        }

        public override string ToString() => $"{nameof(cUIDStoreFeedbackItem)}({UID},{Fetched},{Modified})";
    }

    public class cUIDStoreFeedback : List<cUIDStoreFeedbackItem>
    {
        public cUIDStoreFeedback() { }
        public cUIDStoreFeedback(IEnumerable<cUIDStoreFeedbackItem> pItems) : base(pItems) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIDStoreFeedback));
            foreach (var lItem in this) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }

        public static cUIDStoreFeedback FromUID(cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            var lResult = new cUIDStoreFeedback();
            lResult.Add(new cUIDStoreFeedbackItem(pUID));
            return lResult;
        }

        public static cUIDStoreFeedback FromUIDs(IEnumerable<cUID> pUIDs)
        {
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            uint lUIDValidity = 0;

            foreach (var lUID in pUIDs)
            {
                if (lUID == null) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains nulls");
                if (lUIDValidity == 0) lUIDValidity = lUID.UIDValidity;
                else if (lUID.UIDValidity != lUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains mixed uidvalidities");
            }

            return new cUIDStoreFeedback(from lUID in pUIDs.Distinct() select new cUIDStoreFeedbackItem(lUID));
        }
    }

    public class cStoreFeedback : List<cStoreFeedbackItem>
    {
        public cStoreFeedback() { }
        public cStoreFeedback(IEnumerable<cStoreFeedbackItem> pItems) : base(pItems) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStoreFeedback));
            foreach (var lItem in this) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }

        public static cStoreFeedback FromMessage(cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            var lResult = new cStoreFeedback();
            lResult.Add(new cStoreFeedbackItem(pMessage.Handle));
            return lResult;
        }

        public static cStoreFeedback FromMessages(IEnumerable<cMessage> pMessages)
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

            return new cStoreFeedback(from lHandle in lHandles.Distinct() select new cStoreFeedbackItem(lHandle));
        }
    }
}
