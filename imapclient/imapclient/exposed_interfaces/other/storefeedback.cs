using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Base class for feedback on one message in a store operation.
    /// </summary>
    /// <seealso cref="cStoreFeedbackItem"/>
    /// <seealso cref="cUIDStoreFeedbackItem"/>
    public abstract class cStoreFeedbackItemBase
    {
        /// <summary>
        /// Indicates if a fetch response containing the flags was received for the message during the store operation.
        /// </summary>
        public bool ReceivedFlagsUpdate = false;

        /// <summary>
        /// Indicates if the message was mentioned in the RFC 7162 MODIFIED response code of the store operation.
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

                    if (pHandle.Flags.SymmetricDifference(pFlags, kMessageFlag.Recent).Count() == 0) pSummary.ReflectsOperationCount++;
                    else pSummary.NotReflectsOperationCount++;
                    return;
            }

            pSummary.UnknownCount++; // shouldn't happen 
        }
    }

    /// <summary>
    /// Contains feedback on one message in a store operation.
    /// </summary>
    /// <seealso cref="cStoreFeedback"/>
    public class cStoreFeedbackItem : cStoreFeedbackItemBase
    {
        /**<summary>The message that the feedback relates to.</summary>*/
        public readonly iMessageHandle Handle;

        internal cStoreFeedbackItem(iMessageHandle pHandle)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cStoreFeedbackItem)}({Handle},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    /// <summary>
    /// Contains feedback on a store operation.
    /// </summary>
    /// <seealso cref="cIMAPClient.Store(IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
    public class cStoreFeedback : IReadOnlyList<cStoreFeedbackItem>
    {
        private List<cStoreFeedbackItem> mItems;
        private eStoreOperation mOperation;
        private cStorableFlags mFlags;

        internal cStoreFeedback(iMessageHandle pHandle, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            mItems = new List<cStoreFeedbackItem>();
            mItems.Add(new cStoreFeedbackItem(pHandle));

            mOperation = pOperation;
            mFlags = pFlags;
        }

        internal cStoreFeedback(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cStorableFlags pFlags)
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

        internal cStoreFeedback(IEnumerable<cMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags)
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

        internal bool AllHaveUID => mItems.TrueForAll(i => i.Handle.UID != null);

        /// <summary>
        /// Gets a summary of the feedback. May return a different value after a <see cref="cIMAPClient.Poll"/> .
        /// </summary>
        /// <returns></returns>
        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in mItems) lItem.IncrementSummary(lItem.Handle, mOperation, mFlags, ref lSummary);
            return lSummary;
        }

        /// <summary>
        /// Gets one item.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public cStoreFeedbackItem this[int i] => mItems[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mItems.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cStoreFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
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
    /// Contains a summary of feeback on a store operation.
    /// </summary>
    /// <remarks>
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
    /// <para>Generally <see cref="NotReflectsOperationCount"/> > 0 indicates that a <see cref="cIMAPClient.Poll"/> may be worth trying to get any pending updates from the server (which should convert all the notreflects to expunged or reflects).</para>
    /// <note type="note">After a <see cref="cIMAPClient.Poll"/> you should get the summary again to see the effect of any updates sent by the server.</note>
    /// <para>Generally <see cref="UnknownCount"/> > 0 indicates that a blind store was done so there isn't enough information to say whether the store happened or not.</para>
    /// </remarks>
    /// <seealso cref="cStoreFeedback.Summary"/>
    /// <seealso cref="cUIDStoreFeedback.Summary"/>
    public struct sStoreFeedbackSummary
    {
        /**<summary>The count where an IMAP FETCH for the message was received during the command execution and the message wasn't mentioned in the RFC 7162 MODIFIED response (=> the message was _likely_ to have been updated by the store).</summary>*/
        public int UpdatedCount;

        /**<summary>The count where the message was mentioned in the RFC 7162 MODIFIED response (=> _NOT_ updated by the store).</summary>*/
        public int WasNotUnchangedSinceCount;

        /**<summary>The count where the message cache indicates that the message has been expunged.</summary>*/
        public int ExpungedCount;

        /**<summary>The count where the entry in the message cache for the message can't be found or it can be found but it doesn't contain the flags for the message.</summary>*/
        public int UnknownCount;

        /**<summary>The count where the flags in the message cache reflect the store operation.</summary>*/
        public int ReflectsOperationCount;

        /**<summary>The count where the flags in the message cache do not reflect the store operation.</summary>*/
        public int NotReflectsOperationCount;

        /**<summary>Gets the count of messages that were likely to have been updated by the store.</summary>*/
        public int LikelyOKCount => UpdatedCount + ReflectsOperationCount;
        /**<summary>Gets the count of messages that most likely were NOT updated by the store.</summary>*/
        public int LikelyFailedCount => ExpungedCount + NotReflectsOperationCount;
        /**<summary>Gets the count of messages for which doing a <see cref="cIMAPClient.Poll"/> may increase our knowledge of what happened.</summary>*/
        public bool LikelyWorthPolling => NotReflectsOperationCount > 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(sStoreFeedbackSummary)}(Updated:{UpdatedCount}, WasNotUnchangedSince:{WasNotUnchangedSinceCount}, Expunged:{ExpungedCount}, Unknown:{UnknownCount}, Reflects:{ReflectsOperationCount}, NotReflects:{NotReflectsOperationCount})";
    }

    /// <summary>
    /// Contains feedback on one message in a UID store operation.
    /// </summary>
    /// <seealso cref="cUIDStoreFeedback"/>
    public class cUIDStoreFeedbackItem : cStoreFeedbackItemBase
    {
        /**<summary>The UID that this feedback relates to.</summary>*/
        public readonly cUID UID;
        /**<summary>The message that this feedback relates to, if known. May be <see langword="null"/>.</summary>*/
        public iMessageHandle Handle = null; 

        internal cUIDStoreFeedbackItem(cUID pUID)
        {
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cUIDStoreFeedbackItem)}({UID},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    /// <summary>
    /// Contains feedback on a UID store operation.
    /// </summary>
    /// <seealso cref="cMailbox.UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>
    /// <seealso cref="cMailbox.UIDStore(IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>
    public class cUIDStoreFeedback : IReadOnlyList<cUIDStoreFeedbackItem>
    {
        private List<cUIDStoreFeedbackItem> mItems;
        private eStoreOperation mOperation;
        private cStorableFlags mFlags;

        internal cUIDStoreFeedback(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            mItems = new List<cUIDStoreFeedbackItem>();
            mItems.Add(new cUIDStoreFeedbackItem(pUID));

            mOperation = pOperation;
            mFlags = pFlags;
        }

        internal cUIDStoreFeedback(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags)
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

        /// <inheritdoc cref="cStoreFeedback.Summary" select="summary|returns|remarks"/>
        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in mItems) lItem.IncrementSummary(lItem.Handle, mOperation, mFlags, ref lSummary);
            return lSummary;
        }

        /// <summary>
        /// Gets one item.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public cUIDStoreFeedbackItem this[int i] => mItems[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mItems.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cUIDStoreFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
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
