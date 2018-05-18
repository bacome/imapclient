using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents feedback on one message in a store operation.
    /// </summary>
    /// <seealso cref="cStoreFeedbackItem"/>
    /// <seealso cref="cUIDStoreFeedbackItem"/>
    public abstract class cStoreFeedbackItemBase
    {
        /// <summary>
        /// Indicates whether an IMAP FETCH response containing the flags was received for the message during the store operation.
        /// </summary>
        public bool ReceivedFlagsUpdate = false;

        /// <summary>
        /// Indicates whether the message was mentioned in the RFC 7162 MODIFIED response code of the store operation.
        /// </summary>
        public bool WasNotUnchangedSince = false;

        internal cStoreFeedbackItemBase() { }

        internal void IncrementSummary(iMessageHandle pMessageHandle, eStoreOperation pOperation, cStorableFlags pFlags, ref sStoreFeedbackSummary pSummary)
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

            if (pMessageHandle == null)
            {
                pSummary.UnknownCount++;
                return;
            }

            if (pMessageHandle.Expunged)
            {
                pSummary.ExpungedCount++;
                return;
            }

            if (pMessageHandle.Flags == null)
            {
                pSummary.UnknownCount++;
                return;
            }

            switch (pOperation)
            {
                case eStoreOperation.add:

                    if (pMessageHandle.Flags.Contains(pFlags)) pSummary.ReflectsOperationCount++;
                    else pSummary.NotReflectsOperationCount++;
                    return;

                case eStoreOperation.remove:

                    if (pMessageHandle.Flags.Contains(pFlags)) pSummary.NotReflectsOperationCount++;
                    else pSummary.ReflectsOperationCount++;
                    return;

                case eStoreOperation.replace:

                    if (pMessageHandle.Flags.SymmetricDifference(pFlags, kMessageFlag.Recent).Count() == 0) pSummary.ReflectsOperationCount++;
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
        public readonly iMessageHandle MessageHandle;

        internal cStoreFeedbackItem(iMessageHandle pMessageHandle)
        {
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cStoreFeedbackItem)}({MessageHandle},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    /// <summary>
    /// Contains feedback on a store operation.
    /// </summary>
    /// <seealso cref="cIMAPClient.Store(IEnumerable{cIMAPMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
    public class cStoreFeedback
    {
        public readonly ReadOnlyCollection<cStoreFeedbackItem> Items;
        public readonly eStoreOperation Operation;
        public readonly cStorableFlags Flags;

        internal cStoreFeedback(iMessageHandle pMessageHandle, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));

            var lItems = new List<cStoreFeedbackItem>();
            lItems.Add(new cStoreFeedbackItem(pMessageHandle));
            Items = lItems.AsReadOnly();

            Operation = pOperation;
            Flags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        internal cStoreFeedback(IEnumerable<iMessageHandle> pMessageHandles, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));

            object lMessageCache = null;

            foreach (var lMessageHandle in pMessageHandles)
            {
                if (lMessageHandle == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandles), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (lMessageCache == null) lMessageCache = lMessageHandle.MessageCache;
                else if (!ReferenceEquals(lMessageHandle.MessageCache, lMessageCache)) throw new ArgumentOutOfRangeException(nameof(pMessageHandles), kArgumentOutOfRangeExceptionMessage.ContainsMixedMessageCaches);
            }

            var lItems = new List<cStoreFeedbackItem>(from lMessageHandle in pMessageHandles.Distinct() select new cStoreFeedbackItem(lMessageHandle));
            Items = lItems.AsReadOnly();

            Operation = pOperation;
            Flags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        internal cStoreFeedback(IEnumerable<cIMAPMessage> pMessages, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            object lMessageCache = null;

            cMessageHandleList lMessageHandles = new cMessageHandleList();

            foreach (var lMessage in pMessages)
            {
                if (lMessage == null) throw new ArgumentOutOfRangeException(nameof(pMessages), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                var lMessageHandle = lMessage.MessageHandle;
                if (lMessageCache == null) lMessageCache = lMessageHandle.MessageCache;
                else if (!ReferenceEquals(lMessageHandle.MessageCache, lMessageCache)) throw new ArgumentOutOfRangeException(nameof(pMessages), kArgumentOutOfRangeExceptionMessage.ContainsMixedMessageCaches);
                lMessageHandles.Add(lMessageHandle);

            }

            var lItems = new List<cStoreFeedbackItem>(from lMessageHandle in lMessageHandles.Distinct() select new cStoreFeedbackItem(lMessageHandle));
            Items = lItems.AsReadOnly();

            Operation = pOperation;
            Flags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        internal bool AllHaveUID()
        {
            foreach (var lItem in Items) if (lItem.MessageHandle.UID == null) return false;
            return true;
        }

        /// <summary>
        /// Gets a summary of the feedback. May return a different value after a <see cref="cIMAPClient.Poll"/>.
        /// </summary>
        /// <returns></returns>
        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in Items) lItem.IncrementSummary(lItem.MessageHandle, Operation, Flags, ref lSummary);
            return lSummary;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStoreFeedback));

            lBuilder.Append(Operation);
            lBuilder.Append(Flags);

            if (Items.Count > 0)
            {
                lBuilder.Append(Items[0].MessageHandle.MessageCache);
                foreach (var lItem in Items) lBuilder.Append(lItem.MessageHandle.CacheSequence);
            }

            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Contains a summary of store operation feedback.
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
    /// <para>Generally <see cref="WasNotUnchangedSinceCount"/> + <see cref="ExpungedCount"/> + <see cref="NotReflectsOperationCount"/> is the number of definite non-updates.</para>
    /// <para>Generally <see cref="NotReflectsOperationCount"/> > 0 indicates that a <see cref="cIMAPClient.Poll"/> may be worth trying to get any pending updates from the server (which should convert all the notreflects to expunged or reflects).</para>
    /// <note type="note">After a <see cref="cIMAPClient.Poll"/> you should get the summary again to see the effect of any updates sent by the server.</note>
    /// <para>Generally <see cref="UnknownCount"/> > 0 indicates that a blind store was done so there isn't enough information to say whether the store happened or not.</para>
    /// </remarks>
    /// <seealso cref="cStoreFeedback.Summary"/>
    /// <seealso cref="cUIDStoreFeedback.Summary"/>
    public struct sStoreFeedbackSummary
    {
        /**<summary>The count where an IMAP FETCH response containing the flags for the message was received during the store operation AND the message wasn't mentioned in the RFC 7162 MODIFIED response code of the store operation (=> the message was _likely_ to have been updated by the store).</summary>*/
        public int UpdatedCount;

        /**<summary>The count where the message was mentioned in the RFC 7162 MODIFIED response code of the store operation (=> _NOT_ updated by the store).</summary>*/
        public int WasNotUnchangedSinceCount;

        /**<summary>The count where the message cache indicates that the message has been expunged.</summary>*/
        public int ExpungedCount;

        /**<summary>The count where the entry in the message cache for the message can't be found, or where it can be found but it doesn't contain the flags for the message.</summary>*/
        public int UnknownCount;

        /**<summary>The count where the flags in the message cache reflect the store operation.</summary>*/
        public int ReflectsOperationCount;

        /**<summary>The count where the flags in the message cache do not reflect the store operation.</summary>*/
        public int NotReflectsOperationCount;

        /**<summary>Gets the count of messages that were likely to have been updated by the store.</summary>*/
        public int LikelyOKCount => UpdatedCount + ReflectsOperationCount;
        /**<summary>Gets the count of messages that most likely were NOT updated by the store.</summary>*/
        public int LikelyFailedCount => WasNotUnchangedSinceCount + ExpungedCount + NotReflectsOperationCount;
        /**<summary>Gets the count of messages for which doing a <see cref="cIMAPClient.Poll"/> may increase our knowledge of what happened.</summary>*/
        public bool LikelyWorthPolling => NotReflectsOperationCount > 0;

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(sStoreFeedbackSummary)}(Updated:{UpdatedCount}, WasNotUnchangedSince:{WasNotUnchangedSinceCount}, Expunged:{ExpungedCount}, Unknown:{UnknownCount}, Reflects:{ReflectsOperationCount}, NotReflects:{NotReflectsOperationCount})";
    }

    /// <summary>
    /// Contains feedback on one message in a UID store operation.
    /// </summary>
    /// <seealso cref="cUIDStoreFeedback"/>
    public class cUIDStoreFeedbackItem : cStoreFeedbackItemBase
    {
        /**<summary>The UID that the feedback relates to.</summary>*/
        public readonly cUID UID;
        /**<summary>The message that the feedback relates to. May be <see langword="null"/>.</summary>*/
        public iMessageHandle MessageHandle = null; 

        internal cUIDStoreFeedbackItem(cUID pUID)
        {
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cUIDStoreFeedbackItem)}({UID},{ReceivedFlagsUpdate},{WasNotUnchangedSince})";
    }

    /// <summary>
    /// Contains feedback on a UID store operation.
    /// </summary>
    public class cUIDStoreFeedback
    {
        public readonly ReadOnlyCollection<cUIDStoreFeedbackItem> Items;
        public readonly eStoreOperation Operation;
        public readonly cStorableFlags Flags;

        internal cUIDStoreFeedback(cUID pUID, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));

            var lItems = new List<cUIDStoreFeedbackItem>();
            lItems.Add(new cUIDStoreFeedbackItem(pUID));
            Items = lItems.AsReadOnly();

            Operation = pOperation;
            Flags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        internal cUIDStoreFeedback(IEnumerable<cUID> pUIDs, eStoreOperation pOperation, cStorableFlags pFlags)
        {
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            uint lUIDValidity = 0;

            foreach (var lUID in pUIDs)
            {
                if (lUID == null) throw new ArgumentOutOfRangeException(nameof(pUIDs), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (lUIDValidity == 0) lUIDValidity = lUID.UIDValidity;
                else if (lUID.UIDValidity != lUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDs), kArgumentOutOfRangeExceptionMessage.ContainsMixedUIDValidities);
            }

            var lItems = new List<cUIDStoreFeedbackItem>(from lUID in pUIDs.Distinct() select new cUIDStoreFeedbackItem(lUID));
            Items = lItems.AsReadOnly();

            Operation = pOperation;
            Flags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        /// <inheritdoc cref="cStoreFeedback.Summary" select="summary|returns|remarks"/>
        public sStoreFeedbackSummary Summary()
        {
            sStoreFeedbackSummary lSummary = new sStoreFeedbackSummary();
            foreach (var lItem in Items) lItem.IncrementSummary(lItem.MessageHandle, Operation, Flags, ref lSummary);
            return lSummary;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUIDStoreFeedback));

            lBuilder.Append(Operation);
            lBuilder.Append(Flags);

            if (Items.Count > 0)
            {
                lBuilder.Append(Items[0].UID.UIDValidity);
                foreach (var lItem in Items) lBuilder.Append(lItem.UID.UID);
            }

            return lBuilder.ToString();
        }
    }
}
