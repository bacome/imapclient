using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cStoreFeedbackItemBase
    {
        public bool ReceivedFlagsUpdate = false;
        public bool WasNotUnchangedSince = false;
        public bool Updated => ReceivedFlagsUpdate && !WasNotUnchangedSince;
    }

    public class cStoreFeedbackItem : cStoreFeedbackItemBase
    {
        public readonly iMessageHandle Handle;

        public cStoreFeedbackItem(iMessageHandle pHandle)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
        }

        public bool? Reflects(eStoreOperation pOperation, cSettableFlags pFlags)
        {
            if (Handle.Flags == null) return null; // unknown

            switch (pOperation)
            {
                case eStoreOperation.add:

                    if (Handle.Flags.Contains(pFlags)) return true; // tried to add them, there were there already
                    return false;

                case eStoreOperation.remove:

                    if (Handle.Flags.Contains(pFlags)) return false;
                    return true; // tried to remove them, they weren't there

                case eStoreOperation.replace:

                    if (Handle.Flags.SymmetricDifference(pFlags, kMessageFlagName.Recent).Count() == 0) return true; // tried to replace them, those were the flags that were set
                    return false;
            }

            return null; // should never happen
        }

        public override string ToString() => $"{nameof(cStoreFeedbackItem)}({Handle},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    public class cStoreFeedback : List<cStoreFeedbackItem>
    {
        private cStoreFeedback() { }
        private cStoreFeedback(IEnumerable<cStoreFeedbackItem> pItems) : base(pItems) { }

        public bool AllUpdated => TrueForAll(i => i.Updated);

        public sStoreFeedbackSummary Summary(eStoreOperation pOperation, cSettableFlags pFlags)
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();

            foreach (var lItem in this)
            {
                if (lItem.WasNotUnchangedSince) lSummary.WasNotUnchangedSinceCount++;
                else if (lItem.Handle.Expunged) lSummary.ExpungedCount++;
                else if (lItem.Handle.Flags == null) lSummary.UnknownCount++;
                else if (lItem.Reflects(pOperation, pFlags) == true) lSummary.ReflectsOperationCount++;
                else lSummary.NotReflectsOperationCount++;
            }

            return lSummary;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStoreFeedback));

            if (Count > 0)
            {
                lBuilder.Append(this[0].Handle.Cache);
                foreach (var lItem in this) lBuilder.Append(lItem.Handle.CacheSequence);
            }

            return lBuilder.ToString();
        }

        public static cStoreFeedback FromHandle(iMessageHandle pHandle)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            var lResult = new cStoreFeedback();
            lResult.Add(new cStoreFeedbackItem(pHandle));
            return lResult;
        }

        public static cStoreFeedback FromHandles(IEnumerable<iMessageHandle> pHandles)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));

            object lCache = null;
            cMessageHandleList lHandles = new cMessageHandleList();

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(lHandle), "contains nulls");
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed caches");
                lHandles.Add(lHandle);
            }

            return new cStoreFeedback(from lHandle in lHandles.Distinct() select new cStoreFeedbackItem(lHandle));
        }
    }

    public struct sStoreFeedbackSummary
    {
        public int WasNotUnchangedSinceCount; // condstore failed
        public int ExpungedCount; // message is expunged
        public int UnknownCount; // the handle does not contain the flags
        public int ReflectsOperationCount; // the flags in the handle reflect the update
        public int NotReflectsOperationCount; // the flags in the handle do not reflect the update

        public override string ToString() => $"{nameof(sStoreFeedbackSummary)}(WasNotUnchangedSince:{WasNotUnchangedSinceCount}, Expunged:{ExpungedCount}, Unknown:{UnknownCount}, ReflectsOperation:{ReflectsOperationCount}, NotReflects:{NotReflectsOperationCount})";
    }

    public class cUIDStoreFeedbackItem : cStoreFeedbackItemBase
    {
        public readonly cUID UID;

        public cUIDStoreFeedbackItem(cUID pUID)
        {
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
        }

        public override string ToString() => $"{nameof(cUIDStoreFeedbackItem)}({UID},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    public class cUIDStoreFeedback : List<cUIDStoreFeedbackItem>
    {
        private cUIDStoreFeedback() { }
        private cUIDStoreFeedback(IEnumerable<cUIDStoreFeedbackItem> pItems) : base(pItems) { }

        public bool AllUpdated => TrueForAll(i => i.Updated);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIDStoreFeedback));

            if (Count > 0)
            {
                lBuilder.Append(this[0].UID.UIDValidity);
                foreach (var lItem in this) lBuilder.Append(lItem.UID.UID);
            }

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
}
