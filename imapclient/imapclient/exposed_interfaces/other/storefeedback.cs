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

        public void IncrementSummary(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ref sStoreFeedbackSummary pSummary)
        {
            if (WasNotUnchangedSince)
            {
                pSummary.WasNotUnchangedSinceCount++;
                return;
            }

            if (ReceivedFlagsUpdate)
            {
                pSummary.UpdatedCount++;
                return;
            }

            if (pHandle == null)
            {
                pSummary.UnknownCount++;
                return;
            }

            if (pHandle.Expunged)
            {
                pSummary.FailedCount++;
                pSummary.ExpungedCount++;
                return;
            }

            if (pHandle.Flags == null)
            {
                pSummary.UnknownCount++;
                return;
            }

            switch (pOperation)
            {
                case eStoreOperation.add:

                    if (pHandle.Flags.Contains(pFlags)) pSummary.ReflectsOperationCount++;
                    else
                    {
                        pSummary.FailedCount++;
                        pSummary.NotReflectsOperationCount++;
                    }

                    return;

                case eStoreOperation.remove:

                    if (pHandle.Flags.Contains(pFlags))
                    {
                        pSummary.FailedCount++;
                        pSummary.NotReflectsOperationCount++;
                    }
                    else pSummary.ReflectsOperationCount++;

                    return;

                case eStoreOperation.replace:

                    if (pHandle.Flags.SymmetricDifference(pFlags, kMessageFlagName.Recent).Count() == 0) pSummary.ReflectsOperationCount++;
                    else
                    {
                        pSummary.FailedCount++;
                        pSummary.NotReflectsOperationCount++;
                    }

                    return;
            }

            pSummary.UnknownCount++; // shouldn't happen 
        }
    }

    public class cStoreFeedbackItem : cStoreFeedbackItemBase
    {
        public readonly iMessageHandle Handle;

        public cStoreFeedbackItem(iMessageHandle pHandle)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
        }

        public override string ToString() => $"{nameof(cStoreFeedbackItem)}({Handle},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    public class cStoreFeedback : List<cStoreFeedbackItem>
    {
        private eStoreOperation mOperation;
        private cSettableFlags mFlags;

        private cStoreFeedback(eStoreOperation pOperation, cSettableFlags pFlags)
        {
            mOperation = pOperation;
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        private cStoreFeedback(IEnumerable<cStoreFeedbackItem> pItems, eStoreOperation pOperation, cSettableFlags pFlags) : base(pItems)
        {
            mOperation = pOperation;
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in this) lItem.IncrementSummary(lItem.Handle, mOperation, mFlags, ref lSummary);
            return lSummary;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStoreFeedback));

            lBuilder.Append(mOperation);
            lBuilder.Append(mFlags);

            if (Count > 0)
            {
                lBuilder.Append(this[0].Handle.Cache);
                foreach (var lItem in this) lBuilder.Append(lItem.Handle.CacheSequence);
            }

            return lBuilder.ToString();
        }

        public static cStoreFeedback FromHandle(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            var lResult = new cStoreFeedback(pOperation, pFlags);
            lResult.Add(new cStoreFeedbackItem(pHandle));
            return lResult;
        }

        public static cStoreFeedback FromHandles(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            object lCache = null;

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(lHandle), "contains nulls");
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed caches");
            }

            return new cStoreFeedback(from lHandle in pHandles.Distinct() select new cStoreFeedbackItem(lHandle), pOperation, pFlags);
        }
    }

    public struct sStoreFeedbackSummary
    {
        public int UpdatedCount; // the number where a fetch was received during the command execution and no 'modified' response was received (=> _likely_ to have been updated by the command)
        public int WasNotUnchangedSinceCount; // a 'modified' response was received (=> _NOT_ updated by the command)
        public int FailedCount; // the number where it wasn't updated or wasnotunchangedsince by the above definitions AND either the handle is marked as expunged OR the flags don't reflect the change
        // note that the above three leave an amount of grey: if the handle is null (uid store), does not contain the flags, or does contain flags but they are old, then the result may not be counted in any of the above counts

        public int ExpungedCount; // message is expunged
        public int UnknownCount; // the handle does not contain the flags
        public int ReflectsOperationCount; // the flags in the handle reflect the update
        public int NotReflectsOperationCount; // the flags in the handle do not reflect the update

        public override string ToString() => $"{nameof(sStoreFeedbackSummary)}(Updated:{UpdatedCount}, WasNotUnchangedSince:{WasNotUnchangedSinceCount}, Failed:{FailedCount}, Expunged:{ExpungedCount}, Unknown:{UnknownCount}, Reflects:{ReflectsOperationCount}, NotReflects:{NotReflectsOperationCount})";
    }

    public class cUIDStoreFeedbackItem : cStoreFeedbackItemBase
    {
        public readonly cUID UID;
        public iMessageHandle Handle = null; // filled in after doing the update if possible (otherwise null)

        public cUIDStoreFeedbackItem(cUID pUID)
        {
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
        }

        public override string ToString() => $"{nameof(cUIDStoreFeedbackItem)}({UID},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    public class cUIDStoreFeedback : List<cUIDStoreFeedbackItem>
    {
        private eStoreOperation mOperation;
        private cSettableFlags mFlags;

        private cUIDStoreFeedback(eStoreOperation pOperation, cSettableFlags pFlags)
        {
            mOperation = pOperation;
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        private cUIDStoreFeedback(IEnumerable<cUIDStoreFeedbackItem> pItems, eStoreOperation pOperation, cSettableFlags pFlags) : base(pItems)
        {
            mOperation = pOperation;
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in this) lItem.IncrementSummary(lItem.Handle, mOperation, mFlags, ref lSummary);
            return lSummary;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIDStoreFeedback));

            lBuilder.Append(mOperation);
            lBuilder.Append(mFlags);

            if (Count > 0)
            {
                lBuilder.Append(this[0].UID.UIDValidity);
                foreach (var lItem in this) lBuilder.Append(lItem.UID.UID);
            }

            return lBuilder.ToString();
        }

        public static cUIDStoreFeedback FromUID(cUID pUID, eStoreOperation pOperation, cSettableFlags pFlags)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            var lResult = new cUIDStoreFeedback(pOperation, pFlags);
            lResult.Add(new cUIDStoreFeedbackItem(pUID));
            return lResult;
        }

        public static cUIDStoreFeedback FromUIDs(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cSettableFlags pFlags)
        {
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            uint lUIDValidity = 0;

            foreach (var lUID in pUIDs)
            {
                if (lUID == null) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains nulls");
                if (lUIDValidity == 0) lUIDValidity = lUID.UIDValidity;
                else if (lUID.UIDValidity != lUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains mixed uidvalidities");
            }

            return new cUIDStoreFeedback(from lUID in pUIDs.Distinct() select new cUIDStoreFeedbackItem(lUID), pOperation, pFlags);
        }
    }
}
