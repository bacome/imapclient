using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Base class for feedback on one message from a store operation.
    /// </summary>
    public abstract class cStoreFeedbackItemBase
    {
        /// <summary>
        /// True if a fetch response containing the flags for this message was received during the store operation.
        /// </summary>
        public bool ReceivedFlagsUpdate = false;

        /// <summary>
        /// True if this message was mentioned in the RFC 7162 MODIFIED response code from the store operation.
        /// </summary>
        public bool WasNotUnchangedSince = false;

        internal void IncrementSummary(iMessageHandle pHandle, eStoreOperation pOperation, cStorableFlags pFlags, ref sStoreFeedbackSummary pSummary)
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
                    else pSummary.NotReflectsOperationCount++;
                    return;

                case eStoreOperation.remove:

                    if (pHandle.Flags.Contains(pFlags)) pSummary.NotReflectsOperationCount++;
                    else pSummary.ReflectsOperationCount++;
                    return;

                case eStoreOperation.replace:

                    if (pHandle.Flags.SymmetricDifference(pFlags, kMessageFlagName.Recent).Count() == 0) pSummary.ReflectsOperationCount++;
                    else pSummary.NotReflectsOperationCount++;
                    return;
            }

            pSummary.UnknownCount++; // shouldn't happen 
        }
    }

    /// <summary>
    /// Contains feedback on one message from a store operation.
    /// </summary>
    public class cStoreFeedbackItem : cStoreFeedbackItemBase
    {
        public readonly iMessageHandle Handle;

        public cStoreFeedbackItem(iMessageHandle pHandle)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
        }

        public override string ToString() => $"{nameof(cStoreFeedbackItem)}({Handle},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    /// <summary>
    /// Contains feedback from a store operation.
    /// </summary>
    public class cStoreFeedback : IReadOnlyList<cStoreFeedbackItem>
    {
        private List<cStoreFeedbackItem> mItems;
        private eStoreOperation mOperation;
        private cStorableFlags mFlags;

        public cStoreFeedback(iMessageHandle pHandle, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            mItems = new List<cStoreFeedbackItem>();
            mItems.Add(new cStoreFeedbackItem(pHandle));

            mOperation = pOperation;
            mFlags = pFlags;
        }

        public cStoreFeedback(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            object lCache = null;

            foreach (var lHandle in pHandles)
            {
                if (lHandle == null) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains nulls");
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pHandles), "contains mixed caches");
            }

            mItems = new List<cStoreFeedbackItem>(from lHandle in pHandles.Distinct() select new cStoreFeedbackItem(lHandle));
            mOperation = pOperation;
            mFlags = pFlags;
        }

        public cStoreFeedback(IEnumerable<cMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            object lCache = null;

            cMessageHandleList lHandles = new cMessageHandleList();

            foreach (var lMessage in pMessages)
            {
                if (lMessage == null) throw new ArgumentOutOfRangeException(nameof(pMessages), "contains nulls");
                var lHandle = lMessage.Handle;
                if (lCache == null) lCache = lHandle.Cache;
                else if (!ReferenceEquals(lHandle.Cache, lCache)) throw new ArgumentOutOfRangeException(nameof(pMessages), "contains mixed caches");
                lHandles.Add(lHandle);

            }

            mItems = new List<cStoreFeedbackItem>(from lHandle in lHandles.Distinct() select new cStoreFeedbackItem(lHandle));
            mOperation = pOperation;
            mFlags = pFlags;
        }

        public bool AllHaveUID => mItems.TrueForAll(i => i.Handle.UID != null);

        /// <summary>
        /// Gets a summary of the feedback.
        /// </summary>
        /// <returns>The summary.</returns>
        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in mItems) lItem.IncrementSummary(lItem.Handle, mOperation, mFlags, ref lSummary);
            return lSummary;
        }

        public cStoreFeedbackItem this[int i] => mItems[i];
        public int Count => mItems.Count;
        public IEnumerator<cStoreFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStoreFeedback));

            lBuilder.Append(mOperation);
            lBuilder.Append(mFlags);

            if (mItems.Count > 0)
            {
                lBuilder.Append(mItems[0].Handle.Cache);
                foreach (var lItem in mItems) lBuilder.Append(lItem.Handle.CacheSequence);
            }

            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// <para>A summary of a store operation.</para>
    /// <para>Each message counts towards ONE of;
    /// <list type="bullet">
    /// <item><see cref="UpdatedCount"/></item>
    /// <item><see cref="WasNotUnchangedSinceCount"/></item>
    /// <item><see cref="ExpungedCount"/></item>
    /// <item><see cref="UnknownCount"/></item>
    /// <item><see cref="ReflectsOperationCount"/></item>
    /// <item><see cref="NotReflectsOperationCount"/></item>
    /// </list>
    /// </para>
    /// <para>Generally <see cref="ExpungedCount"/> + <see cref="NotReflectsOperationCount"/> is the number of definite non-updates.</para>
    /// <para>Generally <see cref="NotReflectsOperationCount"/> > 0 indicates that a <see cref="cIMAPClient.Poll"/> may be worth trying to get any pending updates (which should convert all the notreflects to expunged or reflects).</para>
    /// <para><see cref="UnknownCount"/> > 0 indicates that a blind update was done so there isn't enough information to say if the store happened or not.</para>
    /// </summary>
    public struct sStoreFeedbackSummary
    {
        /**<summary>The number where a an IMAP FETCH was received during the command execution and no IMAP MODIFIED response was received (=> _likely_ to have been updated by the command).</summary>*/
        public int UpdatedCount;

        /**<summary>An IMAP MODIFIED response was received (=> _NOT_ updated by the command).</summary>*/
        public int WasNotUnchangedSinceCount;

        /**<summary>The number where the message cache indicates that the message is expunged.</summary>*/
        public int ExpungedCount;

        /**<summary>The number where the internal message handle isn't known (e.g. from a <see cref="cMailbox.UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>) or the message cache does not contain the flags.</summary>*/
        public int UnknownCount;

        /**<summary>The number where the flags in the message cache reflect the update.</summary>*/
        public int ReflectsOperationCount;

        /**<summary>The number where the flags in the message cache do not reflect the update.</summary>*/
        public int NotReflectsOperationCount;

        // calculated values
        public int LikelyOKCount => UpdatedCount + ReflectsOperationCount;
        public int LikelyFailedCount => ExpungedCount + NotReflectsOperationCount;
        public bool LikelyWorthPolling => NotReflectsOperationCount > 0;

        public override string ToString() => $"{nameof(sStoreFeedbackSummary)}(Updated:{UpdatedCount}, WasNotUnchangedSince:{WasNotUnchangedSinceCount}, Expunged:{ExpungedCount}, Unknown:{UnknownCount}, Reflects:{ReflectsOperationCount}, NotReflects:{NotReflectsOperationCount})";
    }

    /// <summary>
    /// Contains feedback on one message from a UID store operation.
    /// </summary>
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

    // TODO: try to work out why the API documentation doesn't generate correctly for the following class (it is missing the Summary method?

    /// <summary>
    /// Contains feedback from a UID store operation.
    /// </summary>
    public class cUIDStoreFeedback : IReadOnlyList<cUIDStoreFeedbackItem>
    {
        private List<cUIDStoreFeedbackItem> mItems;
        private eStoreOperation mOperation;
        private cStorableFlags mFlags;

        public cUIDStoreFeedback(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            mItems = new List<cUIDStoreFeedbackItem>();
            mItems.Add(new cUIDStoreFeedbackItem(pUID));

            mOperation = pOperation;
            mFlags = pFlags;
        }

        public cUIDStoreFeedback(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags)
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

            mItems = new List<cUIDStoreFeedbackItem>(from lUID in pUIDs.Distinct() select new cUIDStoreFeedbackItem(lUID));
            mOperation = pOperation;
            mFlags = pFlags;
        }

        /// <summary>
        /// Gets a summary of the feedback.
        /// </summary>
        /// <returns>The summary.</returns>
        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in mItems) lItem.IncrementSummary(lItem.Handle, mOperation, mFlags, ref lSummary);
            return lSummary;
        }

        public cUIDStoreFeedbackItem this[int i] => mItems[i];
        public int Count => mItems.Count;
        public IEnumerator<cUIDStoreFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIDStoreFeedback));

            lBuilder.Append(mOperation);
            lBuilder.Append(mFlags);

            if (mItems.Count > 0)
            {
                lBuilder.Append(mItems[0].UID.UIDValidity);
                foreach (var lItem in mItems) lBuilder.Append(lItem.UID.UID);
            }

            return lBuilder.ToString();
        }
    }
}
